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
