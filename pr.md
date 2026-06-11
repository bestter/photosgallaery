🔒 [Security] Fix insecure JWT token storage in localStorage

🎯 **What:** The vulnerability fixed
The application was storing sensitive JWT tokens in `localStorage`, which exposed them to potential Cross-Site Scripting (XSS) attacks.

⚠️ **Risk:** The potential impact if left unfixed
If an attacker successfully executed malicious JavaScript on the page (XSS), they could easily read the JWT token from `localStorage` and impersonate the user, gaining unauthorized access to their account and sensitive data.

🛡️ **Solution:** How the fix addresses the vulnerability
The frontend now handles the token via `HttpOnly` cookies. The Axios instance was updated to include `withCredentials: true`, and the `authHelper` was refactored to decode the token upon login, storing only non-sensitive claims (`user_info` such as username, role, and expiration) in `localStorage` for UI purposes.
