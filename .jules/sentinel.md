## 2025-02-27 - Fix Path Traversal in Image Delivery
**Vulnerability:** The application used an insecure `.StartsWith()` check to validate if a generated file path resided within the `PrivateImages` directory. This check can be bypassed, specifically on different OSes depending on trailing slash behavior or directory substring match (e.g., `PrivateImagesOther`). Also, using the unvalidated string (`filePath`) directly in `FileStream` instead of the normalized one (`fullFilePath`) creates a TOCTOU parsing differential vector.
**Learning:** In ASP.NET Core, `Path.GetDirectoryName(fullFilePath) == expectedDir` is universally safer than string prefixes for directory confinement. Always pass the exact normalized path that passed validation directly to the file stream to avoid parsing loopholes.
**Prevention:** Strictly enforce `string.Equals(Path.GetDirectoryName(fullFilePath), expectedRootPath, StringComparison.OrdinalIgnoreCase)` when validating normalized absolute paths, and ensure the `FileStream` exclusively uses this exact `fullFilePath`.

## 2024-05-22 - [HIGH] Fix hardcoded localhost URL in invitation email
**Vulnerability:** Invitation emails were sent with a hardcoded `http://localhost:5173` URL for the frontend application link, which broken the flow in production and posed a URL confusion risk if a user was tricked or an attacker registered a related domain name, and bypassed the configuration defined in the application environment setup.
**Learning:** Hardcoded environment-specific URLs bypass application configuration (`appsettings.json` and environmental variables), leading to broken functionality in production and potential security confusion when users expect the production domain. Configuration was available in the `Program.cs` startup but was omitted in this controller.
**Prevention:** Always inject `IConfiguration` to retrieve application-wide configurations (such as URLs and keys) instead of hardcoding hostnames.

## 2025-02-27 - Fix User Enumeration Vulnerability in Registration
**Vulnerability:** The `Register` endpoint explicitly returned an error indicating whether an account with a given username or email already existed, allowing an attacker to iterate and enumerate valid user accounts.
**Learning:** Explicit error messages on account creation or recovery flows expose system state and can be leveraged by attackers for enumeration and subsequent credential stuffing or phishing attacks.
**Prevention:** To prevent User Enumeration vulnerabilities in authentication endpoints (like user registration or login), do not return explicit error messages indicating that a username or email is already in use. Instead, silently log the duplicate attempt and return a generic success response to mask account existence.
## 2026-05-25 - [Enforce HSTS and HTTPS Redirection]
**Vulnerability:** Missing enforcement of HTTPS and Strict-Transport-Security (HSTS), potentially allowing man-in-the-middle (MitM) attacks or protocol downgrade attacks.
**Learning:** The application was missing explicit configuration to enforce HTTPS communication, relying solely on deployment environment configuration rather than application-level enforcement.
**Prevention:** Explicitly configuring `app.UseHsts()` and `app.UseHttpsRedirection()` in the ASP.NET Core pipeline ensures that the application enforces encrypted transport securely.

## 2025-02-18 - Fix Email Spoofing Vulnerability in Contact Form
**Vulnerability:** Contact forms setting the `From` email address to user-provided input causing SPF/DKIM/DMARC failures and enabling spoofing.
**Learning:** This is a common pattern that frequently fails in production. Email APIs (like Resend) strictly enforce domain verification for the `From` address. Using user input directly results in the email being rejected or marked as spam.
**Prevention:** Always use an application-owned, verified email address in the `From` header. Add the user-provided email to the `Reply-To` header to allow easy responses while maintaining email deliverability and security.

## 2025-03-01 - Add Rate Limiting to ToggleLike Endpoint
**Vulnerability:** The `ToggleLike` endpoint (`[HttpPost("{id}/like")]`) in `PhotosController.cs` lacked rate limiting, allowing authenticated users to spam the like/unlike action, potentially causing unnecessary load and leading to a Denial of Service (DoS) attack on the database.
**Learning:** Even simple toggle actions require rate limiting, especially when they directly hit the database to create or delete records.
**Prevention:** All public and user-facing endpoints that perform state-mutating actions must have explicitly configured rate limiting (e.g., `[EnableRateLimiting]`) using a sensible policy in ASP.NET Core.
## 2024-05-28 - Missing Rate Limiting on Admin Endpoints
**Vulnerability:** Administrative endpoints (`AdminController`, `GroupsController`) in the `PhotoAppApi` backend lacked explicit rate-limiting protection. While they are access-controlled via `[Authorize(Roles = "Admin")]`, this omission left the application vulnerable to internal DoS attacks or abuse if an administrative account were ever compromised, as these endpoints perform state-mutating actions (like creating groups or updating user roles).
**Learning:** Even heavily protected internal or administrative APIs should enforce rate limits as a defense-in-depth measure. Trusting authenticated users (even admins) without constraints violates the principle of least privilege in resource consumption.
**Prevention:** Always apply `[EnableRateLimiting(...)]` to all API endpoints, including administrative ones, establishing a baseline limit (e.g., 30 requests per minute) to contain the blast radius of automated abuse.
## 2026-05-28 - Moderation Bypass Fix
**Vulnerability:** The image upload endpoint skipped all moderation checks and allowed any file if the `ModerationURL` was unconfigured.
**Learning:** Security pipelines should fail-closed. Treating missing security services as a graceful degradation defeats the purpose of the pipeline.
**Prevention:** Explicitly reject requests (fail-closed) when mandatory security dependencies (like the ModerationService) are null or fail, and write unit tests asserting this behavior.
## 2025-03-01 - Missing Rate Limiting on ImagesController
**Vulnerability:** The `ImagesController` exposes resource-intensive endpoints that read files from the disk and interact with external APIs (generating S3 pre-signed URLs). These endpoints lacked rate limiting, making the application vulnerable to Denial of Service (DoS) attacks via resource exhaustion (high disk I/O, memory usage, and network bandwidth consumption).
**Learning:** Even read-only endpoints (GET requests) that interact with the file system or external services need rate limiting protection to prevent automated scraping or DoS attacks from overwhelming the backend.
**Prevention:** Always apply an appropriate `[EnableRateLimiting]` policy to resource-intensive controllers, balancing normal usage needs with protection against aggressive, automated requests.
## 2025-03-01 - Add Rate Limiting to Resource-Intensive GET Endpoints in PhotosController
**Vulnerability:** The `PhotosController` endpoints (`GetPhotos`, `GetUserPhotos`, `GetMostViewedPhotos`) perform resource-intensive queries involving multiple tables and translations, making them potential targets for Denial of Service (DoS) attacks via resource exhaustion if spammed.
**Learning:** Rate limiting is not just for mutating operations (like POST/PUT); any endpoint that consumes significant server resources (CPU, memory, or database I/O) needs protection to ensure system stability.
**Prevention:** Apply appropriate `[EnableRateLimiting]` policies (e.g., `PhotosGetLimiter`) to resource-intensive GET endpoints, ensuring automated scraping or abusive traffic cannot overwhelm the backend while allowing legitimate users normal access.

## 2025-03-01 - Add Rate Limiting to Unprotected Endpoints
**Vulnerability:** Several endpoints in `GroupRequestsController` (`GetAllGroupRequests`, `DeleteGroupRequest`) and `AuthController` (`GetUserGroups`) lacked explicit rate limiting protection. These endpoints perform database queries or deletions and were vulnerable to resource exhaustion or Denial of Service (DoS) attacks if spammed.
**Learning:** All endpoints, including administrative and user profile actions, should have rate limits applied to them. Relying solely on authorization is not sufficient protection against automated abuse or compromised accounts.
**Prevention:** Consistently apply `[EnableRateLimiting]` to all API endpoints, including administrative (`AdminLimiter`) and general GET endpoints (`PhotosGetLimiter`), to establish a baseline protection against resource exhaustion.

## 2024-05-21 - Missing Rate Limiting on Endpoints
**Vulnerability:** Found missing rate limiting attributes on several potentially expensive or sensitive endpoints, notably `DeletePhoto`, `BackfillHashes`, `GenerateMissingThumbnails`, and `MigrateClosedLoop` in `PhotosController`.
**Learning:** Even administrative endpoints or authenticated endpoints manipulating resources need explicit rate limiting to prevent DoS via resource exhaustion or abuse.
**Prevention:** Always ensure `[EnableRateLimiting]` is applied to all endpoints (or via global/controller-level filters) unless explicitly bypassed.

## 2025-02-23 - Prevent IDOR when fallback authenticating UploadPhotos
**Vulnerability:** In `PhotoAppApi/Controllers/PhotosController.cs`, the `UploadPhotos` endpoint relied on `currentUserId.HasValue` combined with `groupId.HasValue` checks but failed to actively deny requests when `groupId.HasValue` was true but `currentUserId.HasValue` was false. This insecure direct object reference (IDOR) allowed the authentication check to be bypassed.
**Learning:** Explicit fallback validation for null claims should always eagerly and actively reject processing if the needed claim for group validation is missing.
**Prevention:** Always verify identity eagerly. When conditional authentication or authorization logic relies on a claim, ensure the absence of that claim results in an immediate 401 or 403, preventing bypasses down the logic chain.

## 2026-06-04 - Unverified Exception Handling Coverage
**Vulnerability:** A catch block returning a generic 500 error in `GroupRequestsController.GetAllGroupRequests` lacked test coverage. Unverified exception handling can mask logging failures, misconfigured exception formats, or regressions where exceptions bubble up instead of being handled gracefully.
**Learning:** Exception handling paths (like catch blocks) in controllers must be explicitly unit tested to ensure they catch expected errors, log appropriately, and return the correct HTTP status code and response schema to the client.
**Prevention:** Intentionally simulate failure states (e.g., disposing the database context to trigger an `ObjectDisposedException` upon querying) within unit tests to trigger and verify `catch` block execution, ensuring both logging and HTTP responses function as designed.

## $(date +%Y-%m-%d) - JWT Token Storage

**Vulnerability:** JWT token stored in `localStorage` in `PhotoFrontend/src/api.js` made it susceptible to Cross-Site Scripting (XSS) attacks.

**Learning:** Sensitive authentication tokens should not be stored in `localStorage` or `sessionStorage` where they can be read by any JavaScript running on the page.

**Prevention:** To prevent XSS exposure, sensitive tokens (like JWT) should be stored in an `HttpOnly`, `Secure` cookie set by the backend API. The frontend can still decode and store non-sensitive user identity claims (like username and role) in `localStorage` for UI rendering, but the actual authentication mechanism should rely on the browser automatically attaching the secure cookie.

## 2025-03-02 - Fix IDOR in ToggleLike
**Vulnerability:** The `ToggleLike` endpoint (`[HttpPost("{id}/like")]` in `PhotosController.cs`) allowed any authenticated user to toggle a like on any photo, including those belonging to private groups they were not members of, by manipulating the photo ID parameter. This was an Insecure Direct Object Reference (IDOR) vulnerability.
**Learning:** Mutating actions on resources that belong to restricted groups must consistently validate the caller's authorization (group membership or admin role) prior to performing the action.
**Prevention:** Explicitly validate group membership using an efficient database query (e.g., `AnyAsync` against the `UserGroups` table) whenever a user attempts to interact with a group-associated resource, and eagerly return `Forbid()` for unauthorized users.
