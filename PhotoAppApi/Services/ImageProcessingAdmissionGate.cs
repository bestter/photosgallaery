using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoAppApi.Services
{
    public class ImageProcessingAdmissionGate : IImageProcessingAdmissionGate
    {
        private readonly object _lock = new();
        private bool _isActive;
        private readonly Queue<TaskCompletionSource<bool>> _waitQueue = new();
        private const int MaxWaiters = 2;
        private static readonly TimeSpan MaxWaitDuration = TimeSpan.FromSeconds(30);

        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<bool>? waitTcs = null;

            lock (_lock)
            {
                if (!_isActive)
                {
                    _isActive = true;
                    return new AdmissionLease(this);
                }

                if (_waitQueue.Count >= MaxWaiters)
                {
                    throw new ImageProcessingBusyException(
                        "Le serveur est actuellement saturé par le traitement d'images. Veuillez réessayer plus tard.",
                        isQueueFull: true
                    );
                }

                waitTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waitQueue.Enqueue(waitTcs);
            }

            using var timeoutCts = new CancellationTokenSource(MaxWaitDuration);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                using (linkedCts.Token.Register(() => waitTcs.TrySetCanceled(linkedCts.Token)))
                {
                    await waitTcs.Task.ConfigureAwait(false);
                }

                return new AdmissionLease(this);
            }
            catch (OperationCanceledException)
            {
                lock (_lock)
                {
                    // If waitTcs was completed successfully right before cancel/timeout was handled,
                    // release the slot to prevent deadlocking the next waiter.
                    if (waitTcs.Task.IsCompletedSuccessfully)
                    {
                        ReleaseSlot();
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new ImageProcessingBusyException(
                    "Le délai d'attente pour le traitement de l'image a expiré. Veuillez réessayer plus tard.",
                    isTimeout: true
                );
            }
        }

        internal void ReleaseSlot()
        {
            lock (_lock)
            {
                while (_waitQueue.Count > 0)
                {
                    var nextTcs = _waitQueue.Dequeue();
                    if (nextTcs.TrySetResult(true))
                    {
                        // The next queued request acquires the slot; _isActive stays true
                        return;
                    }
                }

                _isActive = false;
            }
        }

        private sealed class AdmissionLease : IDisposable
        {
            private ImageProcessingAdmissionGate? _gate;

            public AdmissionLease(ImageProcessingAdmissionGate gate)
            {
                _gate = gate;
            }

            public void Dispose()
            {
                var gate = Interlocked.Exchange(ref _gate, null);
                gate?.ReleaseSlot();
            }
        }
    }
}
