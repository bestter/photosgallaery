## 2025-02-14 - Fix Missing CSRF Token Verification in React Frontend
**Vulnerability:** The ASP.NET Core backend implemented CSRF protection using `AddAntiforgery` and `AutoValidateAntiforgeryTokenAttribute`, requiring an `X-CSRF-TOKEN` on mutating endpoints. However, the React frontend (`client-photo/client-photo/src/api.js`) lacked logic to fetch and transmit this token.
**Learning:** Even if a backend is configured to use CSRF tokens via headers, SPAs (React/Axios) must explicitly be configured to request the token from a dedicated endpoint and inject it into the headers of all mutating requests (POST, PUT, DELETE, PATCH). Relying only on `X-App-Client` headers for protection is insufficient, as the backend's `AutoValidateAntiforgeryTokenAttribute` will reject requests missing the valid CSRF token, or if the backend isn't enforcing it, it leaves the application vulnerable to CSRF.
**Prevention:** When setting up a decoupled SPA and backend, always explicitly fetch the CSRF token on initialization or before mutating requests, and configure the HTTP client (like Axios) to append it to the appropriate request header configured on the backend.

2026-08-20 - Fix IDOR in Photo Deletion
Vulnerability: Insecure Direct Object Reference (IDOR) allowed non-group members to delete photos belonging to a group they were not part of via the `DELETE /api/photos/{id}` endpoint.
Learning: IDOR prevention code was already correctly applied to liking and reporting endpoints but overlooked in deletion. Group membership is tracked in `UserGroups`.
Prevention: Ensure all endpoints handling entities associated with groups (like `Photos`) validate user group membership (via `UserGroups` table) before performing actions, unless the user is an Admin or explicitly authorized by another role.
