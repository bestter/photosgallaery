
## 2026-05-14 - Migrate all backend logging to log4net
**Learning:** Encountered `MissingMethodException` errors in tests (`ArgumentIsEmpty(System.Object)`) stemming from version mismatch between `Microsoft.EntityFrameworkCore.InMemory` and `Pomelo.EntityFrameworkCore.MySql` (used in the API) where EF core versions were pushed to `10.0.8`.
**Action:** Reverting the tests version to the previous working state or manually overriding missing configuration dependencies on startup for `WebApplicationFactory` ensures tests correctly execute without `ConnectionStrings` runtime issues.

## 2025-05-14 - Dead Code Removal in PhotosController

**Learning:** Commented-out code and its associated unused local variables (like `rootPath` and `uploadsFolder` in `UploadPhotos`) can clutter the codebase and trigger unused variable warnings.
**Action:** Removed the commented-out directory creation logic and redundant variables from `PhotoAppApi/Controllers/PhotosController.cs` to improve readability and maintainability.
