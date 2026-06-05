🎯 **What:**
The CORS policy in `PhotoAppApi/Program.cs` was configured to use `.AllowAnyHeader()` and `.AllowAnyMethod()`, which is an overly broad policy. This PR replaces it with explicitly allowed HTTP methods (`GET`, `POST`, `PUT`, `DELETE`, `OPTIONS`) and specific request headers (`Authorization`, `Content-Type`, `Accept`, `X-App-Client`). It also exposes the `X-Total-Count` header required by the frontend for pagination.

⚠️ **Risk:**
Using `.AllowAnyHeader()` and `.AllowAnyMethod()` creates an overly permissive CORS policy. This broad configuration increases the attack surface, potentially allowing unintended cross-origin requests, including unsafe HTTP methods and arbitrary custom headers, to reach the API.

🛡️ **Solution:**
The solution adheres to the principle of least privilege by explicitly defining the allowed methods and headers in the CORS policy.
- Replaced `.AllowAnyMethod()` with `.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")`.
- Replaced `.AllowAnyHeader()` with `.WithHeaders("Authorization", "Content-Type", "Accept", "X-App-Client")`.
- Added `.WithExposedHeaders("X-Total-Count")` to ensure the frontend can read the pagination metadata.
