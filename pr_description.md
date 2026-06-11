🚨 **Severity:** CRITICAL

💡 **Vulnerability:** The application was storing sensitive JWT tokens in `localStorage`, which exposed them to potential Cross-Site Scripting (XSS) attacks.

🎯 **Impact:** If an attacker successfully executed malicious JavaScript on the page (XSS), they could easily read the JWT token from `localStorage` and impersonate the user, gaining unauthorized access to their account and sensitive data.

🔧 **Fix:** The authentication mechanism was updated to use `HttpOnly` and `Secure` cookies.
- The C# Backend (`AuthController`) now sets an `HttpOnly` cookie containing the JWT upon successful login.
- The React Frontend (`axiosInstance`) was configured to send credentials automatically (`withCredentials: true`), allowing the browser to attach the secure cookie to API requests without exposing it to JavaScript.
- The Frontend `authHelper.js` now decodes the token upon login and stores only non-sensitive claims (`user_info`) in `localStorage` for UI purposes (like displaying the user's role and username), ensuring the actual authentication token remains secure.
- Fixed `UploadPhoto.jsx` which was mistakenly relying on an old \`token\` property.

✅ **Verification:**
- Validated `UploadPhoto.jsx` correctly retrieves user identity and session validation without referencing \`token\`.
- Cleaned up any residual insecure `localStorage.getItem('token')` usages in the frontend directories.
- Tests pass via `pnpm test --run`.
