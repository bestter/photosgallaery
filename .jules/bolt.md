## 2024-05-23 - Safe Concurrency for File Hashes
**Learning:** Offloading sequential file I/O operations (like computing SHA-512 hashes) to unbounded `Task.Run` + `Task.WhenAll` can crash the application by causing File Descriptor Exhaustion (too many open files) and thread pool starvation. It also creates synchronous blocking reads if `FileStream` is instantiated synchronously.
**Action:** Use bounded concurrency with `Parallel.ForEachAsync(..., new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, ...)` and ensure the stream uses `useAsync: true` alongside thread-safe collections like `ConcurrentBag` when doing file I/O over collections.
## 2024-05-23 - Bounded Concurrency Array Order
**Learning:** When using `Parallel.ForEachAsync` to bound concurrency safely, `ConcurrentBag` will scramble the order of the results compared to the original inputs.
**Action:** When order matters, use a pre-sized array instead of a `ConcurrentBag` and assign elements concurrently by their original indices (e.g., `Enumerable.Range(0, collection.Count)`). This is a thread-safe operation in C#.
## 2026-05-23 - Efficient IQueryable Subqueries
**Learning:** Calling `await ... ToListAsync()` on an Entity Framework Core query to retrieve a list of IDs to be used in a subsequent `.Contains()` filter causes the framework to fetch the entire list into application memory and execute an inefficient N+1 query pattern.
**Action:** Retain the ID list as an `IQueryable` (without calling `ToListAsync()`). EF Core will then natively translate the `.Contains()` clause into an efficient single SQL `IN` or `EXISTS` subquery, avoiding unnecessary in-memory data transfers.
## 2024-05-23 - Bulk Delete Performance Optimization
**Learning:** Using `.Where(...).ToListAsync()` to fetch entities into application memory and then passing them to `.RemoveRange(...)` is inefficient for bulk deletes, as it adds significant database roundtrips and memory overhead.
**Action:** Use `.ExecuteDeleteAsync()` (or `.ExecuteUpdateAsync()`) introduced in EF Core 7+ for bulk operations to issue a direct SQL DELETE statement without tracking or loading entities into memory.
## 2024-05-23 - Offloading Set Differences to the Database
**Learning:** Fetching multiple entire datasets into memory (e.g. all users and all user group memberships) to compute set differences in C# (e.g. finding users without a specific membership) causes severe memory bloat and performance degradation as the tables grow.
**Action:** Push the set difference logic to the database by using an EF Core `.Any()` subquery (e.g. `_context.Users.Where(u => !_context.UserGroups.Any(...))`). This evaluates entirely via an efficient SQL `NOT EXISTS` query.
## 2024-05-23 - EF Core Pagination Translation
**Learning:** Using `.Skip()` and `.Take()` in EF Core for server-side pagination without an explicit `.OrderBy()` clause causes an `InvalidOperationException` at runtime. Relational databases require a sorted result set to properly translate the `OFFSET/FETCH` query.
**Action:** Always precede `.Skip()` with a deterministic ordering clause (like `.OrderByDescending(x => x.CreatedAt)`) when applying server-side pagination.
## 2024-05-23 - Stale Closures in Asynchronous State Updates
**Learning:** When appending fetched paginated data to an array inside a React component using an async function, directly using the state variable (`setPhotos([...photos, ...newData])`) causes a stale closure if multiple fetches are triggered quickly.
**Action:** Always use functional state updates (`setPhotos(prev => [...prev, ...newData])`) to guarantee the most current state is preserved and appended accurately.

## 2026-05-29 - Prevent Cartesian Explosion with AsSplitQuery
**Learning:** When using Entity Framework Core's `.Include()` and `.ThenInclude()` on multiple or nested collections, the default `.AsSingleQuery()` behavior creates massive SQL JOINs leading to Cartesian explosions. This bloats memory usage and network transfer size, causing severe performance bottlenecks.
**Action:** Always append `.AsSplitQuery()` to LINQ queries fetching entities with multiple one-to-many relationships to instruct EF Core to issue separate, optimized SQL queries.
## 2026-05-30 - Optimize Aggregations with DB Pushdown
**Learning:** Fetching an entire table into application memory (e.g., using `await _context.ImageReports.AsNoTracking().ToListAsync()`) just to perform simple aggregations like counting in C# creates severe memory bloat and slow execution times as the table grows.
**Action:** When calculating counts or simple aggregations, use Entity Framework Core's aggregate functions like `.CountAsync()` to push the computation to the database, leveraging efficient SQL `COUNT` operations.
## 2026-05-30 - Optimize PhotoViewProcessingWorker mass update
**Learning:** I learned that even when using ExecuteUpdateAsync to skip EF Core tracking, invoking it in a loop results in an N+1 query problem, creating heavy database I/O for batch operations. Grouping by the updated property values and using a `.Contains` filter allows combining updates into a much smaller number of SQL statements.
**Action:** Refactored `PhotoViewProcessingWorker` to group view count increments by the added amount and apply `ExecuteUpdateAsync` to all corresponding photo IDs in a single query.
## 2026-05-31 - Safe API Pagination Contract
**Learning:** When converting an existing endpoint from returning an unpaginated flat array to a paginated one, modifying the JSON return body (e.g. from `[]` to `{ items: [], totalCount: 0 }`) is a breaking API change that will crash connected clients expecting an array.
**Action:** Preserve the flat array in the JSON response body and safely append pagination metadata to the HTTP response headers (e.g. `Response.Headers.Append("X-Total-Count", totalCount.ToString())`). Always use `.Append` instead of `.Add` to avoid exceptions if the header is already registered.
## 2025-10-24 - Database I/O Optimization for Counts
**Learning:** Entity Framework Core evaluates `.ToListAsync()` followed by `.Count()` in memory, which is a performance bottleneck for retrieving row counts, particularly large collections.
**Action:** Replaced in-memory evaluation with database aggregate functions like `.CountAsync()` and `Response.Headers.Append("X-Total-Count")` to improve I/O efficiency, following the provided codebase anti-patterns.
## 2026-06-03 - Bounded Concurrency Array Order
**Learning:** When using `Parallel.ForEachAsync` to bound concurrency safely, `ConcurrentBag` will scramble the order of the results compared to the original inputs.
**Action:** When order matters, use a pre-sized array instead of a `ConcurrentBag` and assign elements concurrently by their original indices (e.g., `Enumerable.Range(0, collection.Count)`). This is a thread-safe operation in C#.
## 2024-05-23 - Bounded Concurrency S3 Objects
**Learning:** Fetching an unbounded number of S3 objects concurrently using `Task.WhenAll` can lead to socket exhaustion and large memory spikes, degrading performance when loading configuration.
**Action:** Always use `Parallel.ForEachAsync` to bound the concurrency when doing batch external IO or S3 object reading. This applies identically to file system reading or external service batch fetches.
## 2025-06-05 - Optimize Thumbnail Generation File IO
**Learning:** Doing synchronous `File.Exists` calls inside a loop can be a performance bottleneck, particularly for a large number of files. Batch checking using `Directory.EnumerateFiles` and `HashSet` eliminates individual IO overhead, drastically improving execution times.
**Action:** Refactored `GenerateMissingThumbnails` in `PhotosController.cs` to fetch directory contents upfront using `Directory.EnumerateFiles` and loaded them into HashSets for O(1) lookups during the foreach iteration. Benchmarks showed a measurable decrease in overall loop execution time.
## 2026-06-05 - Avoid Sync-over-Async Task.Run wrappers
**Learning:** Wrapping a blocking asynchronous call in `Task.Run(...).GetAwaiter().GetResult()` from a synchronous method (like when implementing a sync interface) is an anti-pattern. It unnecessarily consumes two thread pool threads: one to execute the Task.Run delegate and one blocking on the result, which exacerbates thread pool starvation.
**Action:** When forced to perform sync-over-async, invoke the method directly and block on the returned task: `AsyncMethod().GetAwaiter().GetResult()`.
## 2024-06-08 - Parallelizing Synchronous Image Processing
**Learning:** Loading the entire entity table into memory (e.g. `await _context.Photos.ToListAsync()`) just to read a single column like `FileName` creates severe memory bloat and latency. Furthermore, processing thousands of images sequentially is heavily CPU/IO bound and under-utilizes available system resources, resulting in massive execution times.
**Action:** Always project only the required fields (e.g., `.Select(p => p.FileName)`) using `AsNoTracking()` to minimize the memory footprint. For batch CPU/IO bound tasks like image processing, use bounded concurrency via `Parallel.ForEachAsync` coupled with `Interlocked.Increment` for thread-safe counters to drastically improve throughput.
## 2026-06-11 - Performance Regression with Nested Array Scans in React Loops

**Learning:** When dealing with nested loops inside React components (e.g. `.map()` over items, and then `.find()` over a property of those items), `Array.prototype.find()` incurs significant overhead due to callback execution and continuous linear array scanning. When processing thousands of items, this `O(N*M)` complexity becomes a noticeable bottleneck.

**Action:** Replace nested array scanning inside `.map()` loops by pre-computing a shared dictionary (`Map`) of derived values *before* the loop. Use unique identifiers (e.g. `tag.id` or `JSON.stringify(tag)`) as keys to map to the resolved values, transforming the inner scan into an `O(1)` dictionary lookup.
