## 2024-05-10 - Add tests for AdminController.GetAllUsers authorization
**Learning:** Addressed missing testing gap by creating tests for the authorization component in the controller class logic. Testing `[Authorize]` attribute properties using Reflection is a reliable way to make sure endpoints are secured without doing full end-to-end testing which requires setting up test servers or mock authorization services.
**Action:** Added `AdminController_RequiresAdminAuthorization` to check if `AdminController` has `[Authorize(Roles = "Admin")]`.
