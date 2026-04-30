## 2025-04-25 - Redundant User Lookups from JWT and Missing AsNoTracking
**Learning:** The application uses JWT for authentication, which includes the user ID (`ClaimTypes.NameIdentifier`) in the claims. However, in heavily used endpoints like `GetPhotos`, the backend was performing a database lookup on the `Users` table by username (`User.Identity?.Name`) multiple times per request to retrieve the user ID or user entity for filtering group access and photo likes. Furthermore, read-only queries were missing `.AsNoTracking()`.
**Action:** Extract the user ID directly from the JWT claims using `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` to avoid the initial `Users` table lookup. Apply `.AsNoTracking()` to read-only EF Core queries to eliminate change tracking overhead, which significantly reduces memory usage and speeds up read operations.
## 2025-02-17 - Avoid Redundant Database Lookups for Current User ID

**Learning:** In ASP.NET Core APIs using JWT authentication, fetching the entire user record from the database (e.g., `await _context.Users.SingleOrDefaultAsync`) solely to retrieve the user's ID is a common N+1/redundant query bottleneck. The C# `ClaimsPrincipal` already contains this information.

**Action:** Whenever a controller endpoint needs the authenticated user's ID, always extract it directly from the token claims using `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` instead of querying the database. This bypasses an unnecessary database roundtrip and saves resources.
## 2024-05-18 - Avoid loading full EF models for simple authorization checks
**Learning:** Found that `ImagesController` queries the entire `Photo` table row (~15 columns) sequentially per image fetched only to read the `GroupId` for checking user access rights. This means every time the gallery was scrolled, a huge amount of unneeded string parsing, memory allocation, and GC overhead happened behind the scenes.
**Action:** Use `.Where().Select(x => new { x.ColumnNeeded }).FirstOrDefaultAsync()` to project only the strictly required column and avoid full entity tracking and materialization when simple values (like foreign keys) are all that are needed.
