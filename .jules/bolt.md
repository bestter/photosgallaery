## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
YYYY-MM-DD - Concurrent I/O Limits in PhotoAppApi
Learning: Unbounded `Task.WhenAll` combined with `Task.Run` for concurrent I/O or network operations (like generating S3 presigned URLs for many images) in ASP.NET Core causes thread pool starvation. It creates a thread per item, exhausting the thread pool limits.
Action: Replace unbounded `Task.WhenAll` operations that execute I/O work with `Parallel.ForEachAsync` with a configured `MaxDegreeOfParallelism` (e.g. `Environment.ProcessorCount`) for safe, bounded concurrency.
