using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;

namespace PhotoAppApi.Services
{
    public class PhotoViewProcessingWorker : BackgroundService
    {
        private readonly ChannelReader<PhotoViewEvent> _channelReader;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PhotoViewProcessingWorker> _logger;
        private readonly int _batchSize = 100; // Limite du lot
        private readonly TimeSpan _maxWaitTime = TimeSpan.FromSeconds(30); // Délai max avant le flush

        public PhotoViewProcessingWorker(
            ChannelReader<PhotoViewEvent> channelReader,
            IServiceProvider serviceProvider,
            ILogger<PhotoViewProcessingWorker> logger)
        {
            _channelReader = channelReader;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Le Worker de traitement des vues est démarré.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var batch = new List<PhotoViewEvent>();

                try
                {
                    // Timer qui annule la lecture au bout de 30 secondes
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(_maxWaitTime);

                    // 1. Lire les événements jusqu'à atteindre la taille du batch ou l'expiration du temps
                    while (batch.Count < _batchSize)
                    {
                        var evt = await _channelReader.ReadAsync(cts.Token);
                        batch.Add(evt);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Le délai _maxWaitTime est écoulé, on attrape l'exception et on relance le traitement.
                }

                // 2. Traiter le batch s'il contient des vues
                if (batch.Any())
                {
                    await ProcessBatchAsync(batch, stoppingToken);
                }
            }
        }

        private async Task ProcessBatchAsync(List<PhotoViewEvent> batch, CancellationToken stoppingToken)
        {
            try
            {
                // AppDbContext est 'Scoped', on doit donc créer un scope depuis le 'Singleton' BackgroundService
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Agréger les incrémentations par PhotoId (ex: on n'update qu'une seule fois si une image est vue 50 fois)
                var increments = batch
                    .GroupBy(v => v.PhotoId)
                    .Select(g => new { PhotoId = g.Key, ViewCountToAdd = g.Count() })
                    .ToList();

                var viewLogs = batch.Select(v => new PhotoView
                {
                    PhotoId = v.PhotoId,
                    UserId = v.UserId,
                    Timestamp = v.Timestamp,
                    IpAddress = v.IpAddress,
                    UserAgent = v.UserAgent
                }).ToList();

                // 2. Utilisation de la stratégie d'exécution pour gérer EnableRetryOnFailure
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);

                    // A. INSERT de masse avec Bulk (.AddRangeAsync est performant géré par EF Core 8)
                    await dbContext.PhotoViews.AddRangeAsync(viewLogs, stoppingToken);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    // B. UPDATE massif des compteurs (Extrêmement rapide via ExecuteUpdateAsync : pas de Tracking EF)
                    foreach (var inc in increments)
                    {
                        await dbContext.Photos
                            .Where(p => p.Id == inc.PhotoId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewsCount, p => p.ViewsCount + inc.ViewCountToAdd), stoppingToken);
                    }

                    // 3. Valider la transaction atomique (Tout ou Rien)
                    await transaction.CommitAsync(stoppingToken);
                });

                _logger.LogInformation($"✅ Lot de {batch.Count} vues traité et enregistré en DB.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur critique lors de la transaction du lot de vues.");
            }
        }
    }
}
