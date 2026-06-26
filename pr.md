🛡️ Sentinel: [CRITICAL/HIGH] Fix DoS via Synchronous CPU-Bound Operation

🚨 Severity: CRITICAL/HIGH

💡 Vulnerability:
Synchronous execution of CPU-bound tasks like `BCrypt.HashPassword` and `BCrypt.Verify` in ASP.NET Core controllers blocked the thread pool, leading to thread starvation and a potential Denial of Service (DoS) when subjected to concurrent requests.

🎯 Impact:
An attacker could cause the service to become unresponsive to all requests by intentionally spamming the `/api/Auth/login` or `/api/Auth/register` endpoints, starving the application's underlying ThreadPool due to long-running, blocking CPU tasks.
🧪 [testing improvement] Add tests for ContactController

🎯 **What:** The testing gap addressed is the lack of unit tests for the `ContactController`. The issue of not validating the ModelState when tests invoke the method directly is fixed. xUnit warnings about using null literals in string parameters are resolved.

📊 **Coverage:** The scenarios now tested are valid contact requests, validation of missing required fields, null requests, and invalid state models. The presence of required attributes like `HttpPost` and rate limiting is also verified, as well as handling exceptions in the `IEmailService`.

🔧 Fix:
Offloaded computationally expensive, synchronous CPU-bound cryptographic operations (specifically `BCrypt.Net.BCrypt.HashPassword` and `Verify`) to the ThreadPool by wrapping the calls in `await Task.Run(...)`, effectively freeing up request threads and ensuring overall application responsiveness.

✅ Verification:
1. Ran all tests in `PhotoAppApi.Tests` to verify no functionality or testing assertions were broken.
2. Verified the fix locally to make sure compilation passed.
✨ **Result:** The overall test coverage is improved.
