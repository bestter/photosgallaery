## 2026-05-08 - Added Tests for PhotosController.ReportPhoto
**Learning:** Adding tests to controllers involving complex authorization logic (like checking if a user is part of a group) requires careful mocking of the `ClaimsPrincipal` with appropriate `ClaimTypes.NameIdentifier` and `ClaimTypes.Role`. Also, ensure any required child entities (like `Group` on `UserGroup`) are fully populated when testing in-memory databases with EF Core to avoid missing required properties errors during initialization.
**Action:** Implemented `PhotosControllerReportTests.cs` to test the report logic in `PhotosController.ReportPhoto`. Covered all expected happy and error paths ensuring full test coverage for the reporting capability.
## $(date +%Y-%m-%d) - Add Tests for GroupsController.GetAllGroups
**What:** Added unit tests for `GetAllGroups` in `GroupsControllerTests.cs`.
**Coverage:** Verified that `GetAllGroups` correctly returns a collection of anonymous objects containing group details (like UserCount and PhotoCount) and sorts them descending by CreatedAt. Covered scenarios for populated data and an empty database.
**Result:** Increased test coverage for `GroupsController`, ensuring projection correctness and sorting stability.
