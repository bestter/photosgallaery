🎯 **What:**
Added a unit test for the `ReportPhoto` error path in `PhotosController` to explicitly check the 500 Internal Server Error behavior when a database exception occurs.

📊 **Coverage:**
- The `PhotosController.ReportPhoto` method's `catch (Exception e)` block is now actively verified.
- The controller correctly returns a 500 status code wrapped in an `ObjectResult` when `_context.SaveChangesAsync` throws an exception.

✨ **Result:**
Enhanced the robustness of the unit test suite for the `PhotosController` by validating that unexpected runtime faults correctly bubble into predictable API HTTP 500 responses without crashing the host process.
