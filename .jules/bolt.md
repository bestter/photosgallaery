## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
2024-05-18 - Bounded Concurrency for I/O in Mapping
Learning: Using unbounded `Task.Run` combined with `Task.WhenAll` inside standard LINQ `.Select()` projections for remote I/O (like fetching presigned URLs from S3) can cause thread pool starvation and File Descriptor exhaustion under heavy load in ASP.NET Core applications.
Action: Replace `Task.WhenAll(items.Select(i => Task.Run(...)))` with `Parallel.ForEachAsync` and a configured `MaxDegreeOfParallelism` to enforce bounded concurrency and safely manage system resources.
