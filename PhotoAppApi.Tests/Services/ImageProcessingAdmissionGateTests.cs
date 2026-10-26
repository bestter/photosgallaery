using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests.Services
{
    public class ImageProcessingAdmissionGateTests
    {
        [Fact]
        public async Task AcquireAsync_SingleRequest_AcquiresImmediately()
        {
            var gate = new ImageProcessingAdmissionGate();
            using var lease = await gate.AcquireAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(lease);
        }

        [Fact]
        public async Task AcquireAsync_QueueFull_ThrowsBusyExceptionWithQueueFull()
        {
            var gate = new ImageProcessingAdmissionGate();
            var ct = TestContext.Current.CancellationToken;

            // 1 active request
            var lease1 = await gate.AcquireAsync(ct);

            // Waiter 1 (enqueued)
            var wait1 = gate.AcquireAsync(ct);

            // Waiter 2 (enqueued)
            var wait2 = gate.AcquireAsync(ct);

            // Waiter 3 (queue full -> must throw immediately)
            var ex = await Assert.ThrowsAsync<ImageProcessingBusyException>(() => gate.AcquireAsync(ct));
            Assert.True(ex.IsQueueFull);
            Assert.False(ex.IsTimeout);

            // Cleanup
            lease1.Dispose();
            using var lease2 = await wait1;
            lease2.Dispose();
            using var lease3 = await wait2;
        }

        [Fact]
        public async Task AcquireAsync_ReleasingSlot_AllowsNextInFifo()
        {
            var gate = new ImageProcessingAdmissionGate();
            var executionOrder = new List<int>();

            var ct = TestContext.Current.CancellationToken;
            var lease1 = await gate.AcquireAsync(ct);
            executionOrder.Add(1);

            var task2 = Task.Run(async () =>
            {
                using var lease2 = await gate.AcquireAsync(ct);
                executionOrder.Add(2);
            }, ct);

            var task3 = Task.Run(async () =>
            {
                using var lease3 = await gate.AcquireAsync(ct);
                executionOrder.Add(3);
            }, ct);

            // Small delay to ensure wait queue order is established
            await Task.Delay(50, ct);

            // Release lease 1 -> task 2 must acquire next
            lease1.Dispose();
            await task2;

            await task3;

            Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
        }

        [Fact]
        public async Task AcquireAsync_CancelledCaller_ThrowsOperationCanceledException()
        {
            var gate = new ImageProcessingAdmissionGate();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => gate.AcquireAsync(cts.Token));
        }

        [Fact]
        public async Task AcquireAsync_CancelledWhileWaiting_ThrowsOperationCanceledExceptionAndDoesNotBlockQueue()
        {
            var gate = new ImageProcessingAdmissionGate();

            var ct = TestContext.Current.CancellationToken;

            // Active request
            var lease1 = await gate.AcquireAsync(ct);

            using var cts = new CancellationTokenSource();
            var waitingTask = gate.AcquireAsync(cts.Token);

            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingTask);

            // Ensure other requests can still enqueue and acquire
            var nextTask = gate.AcquireAsync(ct);
            lease1.Dispose();

            using var leaseNext = await nextTask;
            Assert.NotNull(leaseNext);
        }
    }
}
