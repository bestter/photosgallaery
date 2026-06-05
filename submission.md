🔒 [security fix] Fix Insecure Storage of JWT Token in LocalStorage

🎯 **What:** The application was storing sensitive JWT tokens in `localStorage`, which exposed them to potential Cross-Site Scripting (XSS) attacks.

⚠️ **Risk:** If an attacker successfully executed malicious JavaScript on the page (XSS), they could easily read the JWT token from `localStorage` and impersonate the user, gaining unauthorized access to their account and sensitive data.

🛡️ **Solution:** The authentication mechanism was updated to use `HttpOnly` and `Secure` cookies.
- The C# Backend (`AuthController`) now sets an `HttpOnly` cookie containing the JWT upon successful login.
- The React Frontend (`axiosInstance`) was configured to send credentials automatically (`withCredentials: true`), allowing the browser to attach the secure cookie to API requests without exposing it to JavaScript.
- The Frontend `authHelper.js` now decodes the token upon login and stores only non-sensitive claims (`user_info`) in `localStorage` for UI purposes (like displaying the user's role and username), ensuring the actual authentication token remains secure.
