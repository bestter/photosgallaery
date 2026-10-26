using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoAppApi.Services;

public class ImageProcessingBusyException : Exception
{
    public bool IsQueueFull { get; }
    public bool IsTimeout { get; }

    public ImageProcessingBusyException(string message, bool isQueueFull = false, bool isTimeout = false)
        : base(message)
    {
        IsQueueFull = isQueueFull;
        IsTimeout = isTimeout;
    }
}

public interface IImageProcessingAdmissionGate
{
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}

