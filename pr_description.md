🎯 **What:**
The application previously used a single `FrontendUrl` configuration variable for its CORS policy and static files origin reflection. This configuration was blindly trusted, opening the door for a developer or operator to set it to `*` or a comma-separated list of values without proper parsing, which leads to severe CORS bypass vulnerabilities when `AllowCredentials()` is enabled.

⚠️ **Risk:**
If an operator configures the frontend URL as a wildcard (`*`) or inputs an unparsed list of domains, attackers could bypass the Same-Origin Policy. Because `AllowCredentials()` is enabled, a malicious site could exploit this to perform authenticated cross-origin requests (CSRF-style attacks via XHR/Fetch) and extract sensitive user data or perform unauthorized actions on behalf of the user.

🛡️ **Solution:**
1. **Startup Validation:** Added a strict startup check that throws an exception if the parsed frontend URLs contain a wildcard (`*`), preventing the application from booting into a vulnerable state.
2. **Explicit Parsing:** Refactored the `FrontendUrl` configuration reading to support a comma or semicolon-separated list, splitting and trimming trailing slashes into an array of allowed origins.
3. **Safe Origin Reflection:** Updated the Static Files middleware to dynamically check the incoming request's `Origin` header against the parsed array of allowed origins. It safely reflects the origin only if it explicitly matches, or defaults to the primary configured origin, ensuring compliance with browser security specifications for credentialed requests.
