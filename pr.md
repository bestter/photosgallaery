🧪 [testing improvement] Add tests for ContactController

🎯 **What:** The testing gap addressed is the lack of unit tests for the `ContactController`. The issue of not validating the ModelState when tests invoke the method directly is fixed. xUnit warnings about using null literals in string parameters are resolved.

📊 **Coverage:** The scenarios now tested are valid contact requests, validation of missing required fields, null requests, and invalid state models. The presence of required attributes like `HttpPost` and rate limiting is also verified, as well as handling exceptions in the `IEmailService`.

✨ **Result:** The overall test coverage is improved.
