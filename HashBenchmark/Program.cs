using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;

public class Benchmark
{
    public static async Task Main()
    {
        int fileCount = 100;
        int fileSize = 1024 * 1024; // 1MB
        string tempDir = Path.Combine(Path.GetTempPath(), "HashBenchmark");

        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        var files = new string[fileCount];
        var rnd = new Random(42);
        byte[] buffer = new byte[fileSize];

        for (int i = 0; i < fileCount; i++)
        {
            rnd.NextBytes(buffer);
            files[i] = Path.Combine(tempDir, $"file_{i}.dat");
            File.WriteAllBytes(files[i], buffer);
        }

        // Sequential (Baseline)
        var sw = Stopwatch.StartNew();
        using (var sha512 = SHA512.Create())
        {
            foreach (var file in files)
            {
                using (var stream = File.OpenRead(file))
                {
                    var hashBytes = await sha512.ComputeHashAsync(stream);
                    var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"Sequential processing took: {sw.ElapsedMilliseconds} ms");

        // Concurrent (Proposed)
        sw.Restart();
        var hashTasks = files.Select(file => Task.Run(async () =>
        {
            using var stream = File.OpenRead(file);
            using var sha512Local = SHA512.Create();
            var hashBytes = await sha512Local.ComputeHashAsync(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }));
        await Task.WhenAll(hashTasks);
        sw.Stop();
        Console.WriteLine($"Concurrent processing took: {sw.ElapsedMilliseconds} ms");

        // Cleanup
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }
}
