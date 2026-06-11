## 2024-05-23 - Safe Concurrency for File Hashes
**Learning:** Offloading sequential file I/O operations (like computing SHA-512 hashes) to unbounded `Task.Run` + `Task.WhenAll` can crash the application by causing File Descriptor Exhaustion (too many open files) and thread pool starvation. It also creates synchronous blocking reads if `FileStream` is instantiated synchronously.
**Action:** Always prefer bounded concurrency via `Parallel.ForEachAsync` when processing multiple files. Also, always initialize `FileStream` with `useAsync: true` if you intend to perform asynchronous reads (e.g., `ReadAsync` or passing it to `ComputeHashAsync`).
## 2024-03-24 - Cryptographic Hashing Allocation Overhead
**Learning:** Instantiating `SHA512.Create()` and disposing it in a loop creates significant allocation overhead and slows down hashing, even with fast streams or arrays.
**Action:** Always prefer the static helper methods introduced in newer .NET versions, like `SHA512.HashDataAsync()`, which avoid allocating the provider instance and manage internal pooling efficiently.
