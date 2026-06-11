
## 2026-05-14 - Migrate all backend logging to log4net
**Learning:** Encountered `MissingMethodException` errors in tests (`ArgumentIsEmpty(System.Object)`) stemming from version mismatch between `Microsoft.EntityFrameworkCore.InMemory` and `Pomelo.EntityFrameworkCore.MySql` (used in the API) where EF core versions were pushed to `10.0.8`.
**Action:** Reverting the tests version to the previous working state or manually overriding missing configuration dependencies on startup for `WebApplicationFactory` ensures tests correctly execute without `ConnectionStrings` runtime issues.

## 2025-05-14 - Dead Code Removal in PhotosController

**Learning:** Commented-out code and its associated unused local variables (like `rootPath` and `uploadsFolder` in `UploadPhotos`) can clutter the codebase and trigger unused variable warnings.
**Action:** Removed the commented-out directory creation logic and redundant variables from `PhotoAppApi/Controllers/PhotosController.cs` to improve readability and maintainability.

## 2025-05-14 - Fix React-Leaflet Default Marker Icon Bug
**Learning:** React-Leaflet has a classic bug where default marker icons fail to load correctly because `L.Icon.Default` tries to derive the image path automatically and fails when packaged by Webpack/Vite. A common antipattern is to rely on external CDNs like unpkg to serve these missing assets.
**Action:** Implemented a robust local override in `setupLeaflet.js` that deletes the broken `_getIconUrl` prototype and merges `L.Icon.Default` options to explicitly point to local `leaflet/dist/images/marker-icon.png` module imports. This bundles the assets correctly and eliminates external dependencies. Moved this out of component scope (`ImageModal.js`) into application initialization (`index.js`).
