## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs

2024-05-24 - Thread Pool Starvation from Unbounded Task.WhenAll
Learning: Using unbounded `Task.Run` combined with `Task.WhenAll` to generate S3 presigned URLs in high-traffic ASP.NET Core endpoints causes severe thread pool starvation and latency spikes, as it queues massive numbers of unthrottled work items.
Action: Replace unbounded `Task.Run` with bounded `Parallel.ForEachAsync` (setting `MaxDegreeOfParallelism` to `Environment.ProcessorCount`) to efficiently multiplex concurrent I/O operations without overwhelming the underlying thread pool.
