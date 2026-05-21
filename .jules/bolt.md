## 2024-05-23 - Safe Concurrency for File Hashes
**Learning:** Offloading sequential file I/O operations (like computing SHA-512 hashes) to unbounded `Task.Run` + `Task.WhenAll` can crash the application by causing File Descriptor Exhaustion (too many open files) and thread pool starvation. It also creates synchronous blocking reads if `FileStream` is instantiated synchronously.
**Action:** Use bounded concurrency with `Parallel.ForEachAsync(..., new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, ...)` and ensure the stream uses `useAsync: true` alongside thread-safe collections like `ConcurrentBag` when doing file I/O over collections.
## 2024-05-23 - Bounded Concurrency Array Order
**Learning:** When using `Parallel.ForEachAsync` to bound concurrency safely, `ConcurrentBag` will scramble the order of the results compared to the original inputs.
**Action:** When order matters, use a pre-sized array instead of a `ConcurrentBag` and assign elements concurrently by their original indices (e.g., `Enumerable.Range(0, collection.Count)`). This is a thread-safe operation in C#.
