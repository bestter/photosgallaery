🎯 **What:**
Added missing error path unit test for `DeleteGroup` endpoint in `GroupsController`.

📊 **Coverage:**
Now covering the scenario where the database `SaveChangesAsync` throws an internal exception while attempting to delete a group, returning a 500 Internal Server Error.

✨ **Result:**
Increased testing coverage and improved reliability by ensuring database exceptions are handled correctly and don't leak stack traces in the `DeleteGroup` method.
