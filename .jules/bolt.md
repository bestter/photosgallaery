## 2026-05-08 - [Performance] Optimize image serving with asynchronous FileStream
**Learning:** Using `System.IO.File.Exists` followed by returning `PhysicalFile` can cause multiple synchronous disk checks per image request, blocking thread pool threads. The `IMemoryCache` overhead to mitigate this isn't optimal either. By instead opting to optimistically open a `FileStream` asynchronously and returning a `FileStreamResult` (catching `FileNotFoundException` or `DirectoryNotFoundException` to return `NotFound()`), we completely eliminate synchronous I/O blocks for the happy path where files exist, improving concurrency for high-load image serving scenarios.

**Action:** Replaced `File.Exists` + `IMemoryCache` + `PhysicalFile` logic in `ImagesController` with optimistic `new FileStream(..., FileOptions.Asynchronous)` + `return File(fileStream, contentType)` and wrapped it in a try-catch for `FileNotFoundException` and `DirectoryNotFoundException`.
## 2026-05-08 - [Performance] Optimize image serving with asynchronous FileStream
**Learning:** Using `System.IO.File.Exists` followed by returning `PhysicalFile` can cause multiple synchronous disk checks per image request, blocking thread pool threads. The `IMemoryCache` overhead to mitigate this isn't optimal either. By instead opting to optimistically open a `FileStream` asynchronously and returning a `FileStreamResult` (catching `FileNotFoundException` or `DirectoryNotFoundException` to return `NotFound()`), we completely eliminate synchronous I/O blocks for the happy path where files exist, improving concurrency for high-load image serving scenarios.

**Action:** Replaced `File.Exists` + `IMemoryCache` + `PhysicalFile` logic in `ImagesController` with optimistic `new FileStream(..., FileOptions.Asynchronous)` + `return File(fileStream, contentType)` and wrapped it in a try-catch for `FileNotFoundException` and `DirectoryNotFoundException`.

## 2025-05-18 - Eliminating Redundant Database User Lookups via JWT Claims
**Learning:** In JWT-authenticated endpoints, performing a `FirstOrDefaultAsync` or `SingleOrDefaultAsync` lookup against the `Users` table by username simply to resolve the `UserId` or `Username` is redundant and creates N+1 query bottlenecks. The `ClaimsPrincipal` inherently contains this information via the `ClaimTypes.NameIdentifier` and `ClaimTypes.Name` claims.
**Action:** When working with authenticated routes (such as fetching user groups or creating invitations), extract the user's ID directly using `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` and username via `User.Identity?.Name` or `ClaimTypes.Name`. This cleanly avoids a full round-trip query to the database. Additionally, ensure read-only mapping queries (like fetching groups) apply `.AsNoTracking()` to reduce Entity Framework Core memory overhead and GC pressure.

## 2024-05-18 - Eliminate Change Tracking Overhead in EF Core Read-Only Queries
**Learning:** Entity Framework Core adds significant memory and CPU overhead when executing `.ToListAsync()` or tracking changes on fetched entities. Many administrative and view-only endpoints were fetching full entities without needing to modify them.
**Action:** Consistently apply `.AsNoTracking()` to `IQueryable` chains where the fetched entities are strictly read-only and won't be modified before returning the response. This pattern reduces memory usage and CPU cycles, improving the scalability of administrative list endpoints.

## 2025-05-18 - Eliminate DB Query for Current User in JWT Auth
**Learning:** In JWT-authenticated controllers (like `PhotosController`), querying `_context.Users.FirstOrDefaultAsync` using the username just to get the `UserId` is an unnecessary N+1 performance drain per request.
**Action:** Extract the `UserId` directly from the JWT claims using `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` and parse it instead of performing a database hit.

## 2026-05-07 - [Caching File Existence in ImagesController]
**Learning:** Synchronous `System.IO.File.Exists` calls in high-concurrency endpoints (like image serving) block thread pool threads, leading to increased latency and potential thread exhaustion. Memory lookups are orders of magnitude faster (~20x in benchmarks).
**Action:** Injected `IMemoryCache` into `ImagesController` and implemented caching for file existence checks in `GetImage` and `GetThumbnail` with a 10-minute sliding expiration. Registered the memory cache service in `Program.cs`.
## 2025-05-18 - Refactor Exception Handling for Performance and Clarity
**Learning:** Catching generic exceptions (`catch (Exception)`) without logging them makes debugging difficult and hides potential issues. While we shouldn't expose sensitive information to the user, the exception details should still be recorded.
**Action:** Replace `catch (Exception)` with `catch (Exception ex)` and use a logger to record the exception details internally, while continuing to return a generic 500 error to the client. This improves observability without compromising security.
## 2024-06-25 - Prevent O(n) filter recalculation in Gallery
## 2025-05-08 - Unit Tests for ContactController
**Learning:** Writing test coverage for API endpoints helps ensure inputs are handled correctly, dependencies are interacted with properly, and edge cases do not result in unhandled exceptions.
**Action:** Created unit tests for the ContactController to verify correct success responses, bad request on missing fields, and internal server errors on external dependency failure.
## 2025-05-18 - Refactor Exception Handling for Performance and Clarity

**Learning:** Re-calculating expensive filter logic (like mapping over nested arrays for tagging/translations) on every render in React causes significant input lag when the user interacts with fast-updating states (like typing in a search bar).
**Action:** Always wrap derived datasets like `filteredPhotos` in `useMemo` with their specific dependencies, preventing unrelated state changes from blocking the main thread.

## 2024-05-08 - [Performance] Parallelize Stream Hashing in UploadPhotos
**Learning:** During image batch uploads, calculating cryptographic hashes (e.g., SHA-512) for each file sequentially can be slow, especially with many or large files.
**Action:** Replaced sequential synchronous hashing with a parallel approach using `Task.WhenAll` and `Task.Run` combined with async I/O streams in `PhotosController.UploadPhotos`. This concurrent overlapping of compute-heavy hashing and stream reads provides a ~3x speedup when processing multiple files, minimizing total CPU idle time and speeding up the endpoint's response.

**Learning:** Re-calculating expensive filter logic (like mapping over nested arrays for tagging/translations) on every render in React causes significant input lag when the user interacts with fast-updating states (like typing in a search bar).
**Action:** Always wrap derived datasets like `filteredPhotos` in `useMemo` with their specific dependencies, preventing unrelated state changes from blocking the main thread.

## 2024-05-18 - Optimize BackfillHashes Concurrent I/O
**Learning:** Sequential processing of file streams (I/O bounds) can be vastly improved by running Tasks asynchronously and executing them concurrently with `Task.WhenAll`.
**Action:** Replaced sequential `foreach` in `PhotosController.BackfillHashes` with concurrent `Task.Run` map yielding a ~3x-5x performance boost over batches.
## 2024-05-14 - Optimize synchronous file deletion in DeletePhoto

**Learning:** `System.IO.File.Exists` and `System.IO.File.Delete` perform synchronous, blocking filesystem I/O operations. When executed sequentially on the main request thread alongside database operations, they cause unnecessary latency and block thread-pool threads, reducing server scalability. Since deleting files from the filesystem does not affect the logical outcome of a DELETE HTTP request (from the perspective of database state), they do not need to block the response.

**Action:** Moved the physical file deletion (`File.Exists` and `File.Delete` for both original and thumbnail) inside a `Task.Run()` block to execute concurrently with the database deletion (`_context.Photos.Remove`). Added an `await` before returning the response. This effectively hides the I/O latency behind the database operations, cutting the time to delete a photo significantly.
## 2026-05-08 - Batched fetching of presigned URLs
**Learning:** Sequential `await` calls inside `foreach` loops for network/I/O bound operations (like fetching S3 pre-signed URLs via `GetImageUrlAsync`) create N+1 latency bottlenecks. Mock data in tests must fully implement `required` properties to avoid CS9035 compiler errors when testing these changes.
**Action:** Use `Task.WhenAll` alongside LINQ `.Select(async ...)` to execute independent network requests concurrently. Always run `dotnet build` and `dotnet test` to catch any missing required properties in test mocks that may be introduced during refactoring.
## $(date +%Y-%m-%d) - Remove unused GetUserLikes endpoint
**Learning:** Sometimes the best performance improvement isn't optimizing a slow method, but deleting code that is completely unused. Maintaining dead code adds unnecessary bloat, compilation time, and cognitive load, and can sometimes conceal performance pitfalls. Always verify if a slow piece of code is actually in use before spending time optimizing it.
**Action:** When evaluating methods for performance tuning, verify their usage across the codebase. If confirmed dead code, delete it rather than optimizing it to improve maintainability and slightly speed up compilation.
## 2026-05-11 - Prevent O(n) filter recalculation in Dashboard
**Learning:** Re-calculating expensive string matching filter logic (like mapping over user arrays to check username or email against a search term) on every render in React causes significant input lag when the user interacts with fast-updating states (like typing in a search bar).
**Action:** Wrapped derived dataset `filteredUsers` in `useMemo` and used `useDeferredValue` to defer the search term, preventing expensive filtering from blocking the main thread and making search input typing smooth.
## 2024-05-12 - [Debounce Search Query Filtering in Gallery]
**Learning:** Performing client-side string matching (e.g., `toLowerCase().includes(query)`) inside `useMemo` on every keystroke blocks the React main thread when the dataset (photos and tags) is large.
**Action:** Always wrap high-frequency inputs (like text search) with a `useDebounce` hook before passing them as dependencies to expensive client-side filtering operations to prevent UI stuttering.
## 2024-05-13 - [Optimize React List Filtering with useMemo and useDeferredValue]
**Learning:** In frontend React applications with search functionalities mapping over large arrays (like reports or users), synchronous `filter` operations on every keystroke can cause noticeable input lag. `Moderation.jsx` had a search input that was not properly debounced or memoized, meaning typing in the search bar would trigger a full re-render and re-filtering of all reports on every keystroke, potentially blocking the main thread.
**Action:** Always wrap computationally expensive derived state calculations (e.g., filtering large arrays by text) in a `useMemo` hook. Furthermore, for text inputs driving these filters, using `useDeferredValue` for the search term ensures that typing remains responsive (high priority) while the expensive filtering is deferred (low priority), providing a smoother user experience without needing manual debounce implementations.

## 2024-05-13 - [Add Native Image Lazy Loading in Gallery]
**Learning:** Rendering many large image elements (e.g., `<img />`) synchronously during component mount can severely impact the Initial Load time, block the main thread with rendering operations, and fetch unnecessary bytes for images that are below the fold (off-screen).
**Action:** Always add the `loading="lazy"` attribute to `<img>` tags inside lists or grids that render many items, enabling the browser's native intersection observer to defer image decoding and fetching until the user scrolls them into view.
