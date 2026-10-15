## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
## 2026-07-30 - Unit test for PhotosController.MigrateClosedLoop
**Learning:** Wrote unit test for PhotosController.MigrateClosedLoop using in-memory database to simulate pre-migration state. Mapped paths using IWebHostEnvironment, verified file movements locally and correct DB modifications.
**Action:** Wrote MigrateClosedLoop_ShouldMigrateUsersPhotosAndFiles test in PhotosControllerTests.cs
2024-05-24 - Thread Pool Starvation from Unbounded Task.WhenAll
Learning: Using unbounded Task.Run combined with Task.WhenAll to generate S3 presigned URLs in high-traffic ASP.NET Core endpoints causes severe thread pool starvation and latency spikes, as it queues massive numbers of unthrottled work items.
Action: Replace unbounded Task.Run with bounded Parallel.ForEachAsync (setting MaxDegreeOfParallelism to Environment.ProcessorCount) to efficiently multiplex concurrent I/O operations without overwhelming the underlying thread pool.

2026-08-19 - React Memoization on Gallery Bento Grid
Learning: When rendering large lists of images in React, individual card components rendered via mapping should be wrapped in React.memo to prevent heavy DOM reconciliations when unrelated parent state changes.
Action: Look for map functions inside grids or lists where complex React components are rendered without memoization.
2026-10-03 - React List Memoization Dependencies
Learning: When using `React.memo` to prevent unnecessary re-renders of child components in React (like lists), any functions passed as props must be wrapped in `useCallback` in the parent component. Otherwise, a new function reference is created on every parent render, causing the shallow prop comparison to fail.
Action: Always verify and stabilize the references of function props using `useCallback` when applying `React.memo` optimizations to list items.

2026-10-07 - React.memo Optimization
Learning: Inline objects passed as props defeat React.memo optimizations because they create a new reference on every render, causing unnecessary re-renders.
Action: Always wrap object literal props in useMemo when passing them to memoized components.

2026-10-12 - Prevent Inline Objects in React.memo
Learning: Passing an inline object literal to a React component wrapped in React.memo creates a new reference on every parent render, forcing the shallow comparison to always fail and causing unnecessary child re-renders.
Action: Always wrap object literal props in a useMemo hook when passing them to memoized components.
