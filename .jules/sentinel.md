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

## 2025-03-02 - JWT Token Storage

**Vulnerability:** JWT token stored in `localStorage` in `PhotoFrontend/src/api.js` made it susceptible to Cross-Site Scripting (XSS) attacks.

**Learning:** Sensitive authentication tokens should not be stored in `localStorage` or `sessionStorage` where they can be read by any JavaScript running on the page.

**Prevention:** To prevent XSS exposure, sensitive tokens (like JWT) should be stored in an `HttpOnly`, `Secure` cookie set by the backend API. The frontend can still decode and store non-sensitive user identity claims (like username and role) in `localStorage` for UI rendering, but the actual authentication mechanism should rely on the browser automatically attaching the secure cookie.

## 2025-03-02 - Fix IDOR in ToggleLike
**Vulnerability:** The `ToggleLike` endpoint (`[HttpPost("{id}/like")]` in `PhotosController.cs`) allowed any authenticated user to toggle a like on any photo, including those belonging to private groups they were not members of, by manipulating the photo ID parameter. This was an Insecure Direct Object Reference (IDOR) vulnerability.
**Learning:** Mutating actions on resources that belong to restricted groups must consistently validate the caller's authorization (group membership or admin role) prior to performing the action.
**Prevention:** Explicitly validate group membership using an efficient database query (e.g., `AnyAsync` against the `UserGroups` table) whenever a user attempts to interact with a group-associated resource, and eagerly return `Forbid()` for unauthorized users.
## 2025-02-28 - Route and Rate Limit Misconfiguration on Register Endpoint
**Vulnerability:** The `[HttpPost("register")]` route and its associated `[EnableRateLimiting("RegisterLimiter")]` attribute were mistakenly placed on the `Logout` method in `AuthController.cs`.
**Learning:** This resulted in the `Register` method acting as an open route without the intended rate-limiting protection, exposing the system to potential automated account creation spam (DoS) and routing the registration requests incorrectly.
**Prevention:** Always verify that route and security attributes are applied to the correct controller actions, especially after refactoring or code additions. Ensure test coverage checks both routing behavior and the presence of expected rate limit protections on sensitive endpoints.
## 2025-03-02 - Fix HTML Injection in Emails
**Vulnerability:** The `ResendEmailService` constructed HTML emails by directly concatenating user-provided inputs (`name`, `email`, `subject`, `message`, `firstName`, `lastName`, `inviterName`, `groupName`) into a `StringBuilder` without HTML encoding. This created a Cross-Site Scripting (XSS) / HTML Injection vulnerability where attackers could inject deceptive HTML or malicious scripts that render in the recipient's email client.
**Learning:** Even if data isn't rendered directly in the web browser, if it's sent to an email client as part of an HTML body (`HtmlBody`), it must be sanitized. Email clients can interpret injected HTML, leading to severe phishing or XSS risks.
**Prevention:** Always use `System.Net.WebUtility.HtmlEncode()` to sanitize user-provided inputs before embedding them into any HTML context, including HTML emails.
## 2026-06-11 - [HIGH] Fix overly permissive CORS policy
**Vulnerability:** Using .AllowAnyMethod() and .AllowAnyHeader() in the CORS policy is overly broad and exposes endpoints to potentially malicious cross-site requests.
**Learning:** Always restrict CORS policies using .WithMethods() and .WithHeaders() to exactly the expected verbs and fields required by the frontend.
**Prevention:** Hardcode specific headers and methods in the options.AddPolicy builder rather than utilizing catch-all extension methods.
## 2026-06-18 - Fix IDOR in ReportPhoto
**Vulnerability:** The `ReportPhoto` endpoint (`[HttpPost("{id}/report")]` in `PhotosController.cs`) allowed any authenticated user to report any photo, including those belonging to private groups they were not members of, by manipulating the photo ID parameter. This was an Insecure Direct Object Reference (IDOR) vulnerability.
**Learning:** Mutating actions on resources that belong to restricted groups must consistently validate the caller's authorization (group membership or admin role) prior to performing the action.
**Prevention:** Explicitly validate group membership using an efficient database query (e.g., `AnyAsync` against the `UserGroups` table) whenever a user attempts to interact with a group-associated resource, and eagerly return `Forbid()` for unauthorized users.

## 2026-06-25 - Hardcoded Cloud Storage Properties Fix
**Vulnerability:** A hardcoded fallback value `?? "pixellyra"` was used for `ObjectStorage:BucketName` configuration. This can lead to sensitive data (such as Data Protection Keys) being written to or read from a predictable, potentially externally owned, storage bucket, introducing risks of data leakage and sovereignty violations.
**Learning:** Hardcoded fallback values for external cloud infrastructure (e.g. buckets, regions) mask configuration errors and introduce severe security/compliance risks.
**Prevention:** Do not use fallback strings like `?? "fallback"` for external infrastructure locations. Always enforce explicit configuration by checking `string.IsNullOrWhiteSpace` and throwing an `InvalidOperationException` if the necessary configuration is not provided.
## 2026-06-24 - Fix User Enumeration in Invitations
**Vulnerability:** In `InvitationsController`, inviting an existing user returned different API messages depending on their account state, allowing attackers to enumerate registered emails.
**Learning:** Explicitly stating if an invited user already exists or is already in a group leaks identity state. Furthermore, attempting to mitigate timing attacks by using synchronous CPU-bound operations like `BCrypt.HashPassword` on high-traffic endpoints blocks thread pool threads and introduces a Denial-of-Service (DoS) vulnerability.
**Prevention:** Standardize response payloads for all invitation states (success, already invited, user exists) to a single generic message. Avoid using synchronous CPU-bound cryptographic operations as timing attack mitigations in endpoints where they can cause thread starvation.

## 2026-06-25 - Fix Synchronous BCrypt in High-Traffic Endpoints
**Vulnerability:** The `Register` endpoint in `AuthController.cs` mitigated User Enumeration timing attacks by computing `BCrypt.HashPassword` when a user already existed. However, this is a CPU-bound, synchronous operation that allocates new salt every time, creating a Denial-of-Service (DoS) vulnerability in a high-traffic endpoint.
**Learning:** Using `BCrypt.HashPassword` as a dummy operation for timing attack mitigation is overly expensive and blocks thread pool threads.
**Prevention:** To equalize response times for authentication or registration endpoints without generating new salts, use `BCrypt.Verify(password, dummyHash)` against a pre-computed dummy hash instead of `BCrypt.HashPassword(password)`.
## 2026-06-25 - Fix missing CancellationToken in ToggleLike
**Vulnerability:** The `ToggleLike` endpoint (`[HttpPost("{id}/like")]` in `PhotosController.cs`) was already patched for an IDOR vulnerability, but lacked cancellation token support for its database queries.
**Learning:** Endpoints that perform multiple database queries (`FindAsync`, `AnyAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`) can waste database resources and thread pool threads if the client disconnects prematurely. Passing a `CancellationToken` enables the database driver to cancel operations in flight.
**Prevention:** Consistently inject a `CancellationToken cancellationToken = default` parameter into ASP.NET Core API controller methods and thread it down into all asynchronous Entity Framework Core operations.
## 2023-10-27 - Unbounded Memory Read in file uploads
**Vulnerability:** Calling `await file.read()` in FastAPI/Starlette loads the entire file into memory, causing a high risk of Out-Of-Memory (OOM) Denial-of-Service attacks when malicious actors upload extremely large files or use decompression bombs.
**Learning:** File size limits must be checked dynamically while reading the file in chunks rather than relying on content-length headers or reading the entire file first.
**Prevention:** Always use chunked reading loops (e.g., `while True: chunk = await file.read(1MB)`) when processing file uploads, verifying the cumulative size does not exceed the allowed threshold before proceeding.

## 2024-06-25 - DoS via Synchronous CPU-Bound Operation
**Vulnerability:** Synchronous execution of CPU-bound tasks like `BCrypt.HashPassword` and `BCrypt.Verify` in ASP.NET Core controllers blocks the thread pool, leading to thread starvation and a potential Denial of Service (DoS) when subjected to concurrent requests.
**Learning:** Even if a library does not provide native asynchronous methods for computationally heavy tasks, they must not be executed synchronously on the request thread.
**Prevention:** Always offload computationally expensive, synchronous CPU-bound work to the ThreadPool using `await Task.Run(...)` to free up request threads.

## 2026-06-25 - Fix Pillow Decompression Bomb Vulnerability
**Vulnerability:** The NSFW moderation service used `PIL.Image.open` without a properly restrictive `Image.MAX_IMAGE_PIXELS` setting (defaulting to 89 megapixels) and lacked error handling for `DecompressionBombError`. This created a Denial-of-Service (DoS) vulnerability where an attacker could upload extremely large or heavily compressed "bomb" images to exhaust server memory and CPU during decompression.
**Learning:** Python's Pillow (PIL) library defaults to a relatively high pixel limit. For public-facing services that process user uploads, relying on the default limit is insufficient and can lead to OOM crashes.
**Prevention:** Always restrict maximum image dimensions explicitly by setting `PIL.Image.MAX_IMAGE_PIXELS = <safe_limit>` (e.g., 10,000,000 for 10 megapixels) globally at module initialization. Additionally, always wrap image loading in a `try...except Image.DecompressionBombError` block to catch violations gracefully and return a 400 Bad Request rather than propagating a 500 Internal Server Error.

## 2026-06-25 - Fix Unbounded Memory Read / Decompression Bomb Risk
**Vulnerability:** The Python moderation service endpoint using PIL (`Image.open`) was susceptible to a decompression bomb attack, where a maliciously crafted image could consume massive amounts of memory leading to an Out-Of-Memory (OOM) Denial-of-Service condition. In addition, an unhandled exception generated a 500 error instead of failing validation gracefully.
**Learning:** External un-trusted images can expand infinitely into memory without pixel limits. The default behavior in older PIL/Pillow or without limits can be lethal. Pillow does throw a `DecompressionBombError` under some conditions, but it wasn't caught safely in the API.
**Prevention:** Always restrict pixel counts when decoding user-provided images (e.g., `Image.MAX_IMAGE_PIXELS = 10000000`). Make sure validation errors are explicitly caught (like `except Image.DecompressionBombError`) and return 400 Bad Request to indicate an invalid file rather than crashing or throwing 500s.

## 2025-03-02 - Fix missing CancellationToken in maintenance and report endpoints
**Vulnerability:** Several maintenance and reporting endpoints (`ReportPhoto`, `BackfillHashes`, `GenerateMissingThumbnails`, `MigrateClosedLoop`) performed database operations (`FindAsync`, `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`) or expensive file processing (`Parallel.ForEachAsync`) without accepting or passing a `CancellationToken`. This allowed long-running or unbounded operations to continue consuming database resources, memory, and thread pool threads even if the client disconnected prematurely, creating a Denial of Service (DoS) vulnerability via resource exhaustion.
**Learning:** Any endpoint that performs I/O bound operations, multiple database queries, or long-running computations must be able to cancel those operations when the client disconnects.
**Prevention:** Always accept a `CancellationToken cancellationToken = default` parameter in ASP.NET Core API controller methods and propagate it down to all asynchronous Entity Framework Core operations and parallel loops (e.g. `ParallelOptions`).

## 2026-06-25 - Fix Stale JWT Claims Authorization Bypass
**Vulnerability:** The application evaluated role-based permissions (like `Admin` logic or `[Authorize(Roles = "Admin")]`) using only the claims present in the JWT token (specifically the `Role` claim) during validation. This allowed an attacker whose permissions were downgraded or completely removed (e.g. an Admin demoted to User) to retain their elevated privileges for up to 24 hours until the token expired, resulting in a severe Privilege Escalation / Auth Bypass.
**Learning:** Do not rely exclusively on JWT claims (like UserId or Role) for authorization on sensitive operations, as claims represent a snapshot at issuance.
**Prevention:** In `OnTokenValidated`, retrieve the user's current role from the database or an up-to-date cache, and reject the token (using `context.Fail()`) if the token's role claim no longer matches the user's actual database role.
## 2026-06-25 - Fix missing CancellationToken in Photos and UserGroups Endpoints
**Vulnerability:** The endpoints `GetUserPhotos`, `GetMostViewedPhotos` (in `PhotosController.cs`) and `GetUserGroups` (in `AuthController.cs`) executed Entity Framework Core database operations (e.g. `FindAsync`, `CountAsync`, `ToListAsync`, `SingleOrDefaultAsync`) without accepting or passing a `CancellationToken`. This created a Denial of Service (DoS) vulnerability via resource exhaustion, as database queries would continue running even if the client disconnected prematurely.
**Learning:** Any endpoint that performs I/O bound operations, particularly multiple database queries or large aggregations, must be able to cancel those operations when the client disconnects to prevent wasting database connections and thread pool threads.
**Prevention:** Consistently inject a `CancellationToken cancellationToken = default` parameter into ASP.NET Core API controller method signatures and thread it down into all asynchronous Entity Framework Core operations. Also ensure the tests are updated to provide `default` or `TestContext.Current.CancellationToken` where necessary to avoid breaking tests or generating analyzer warnings.
## 2026-06-25 - [MEDIUM] Fix missing CancellationToken
**Vulnerability:** Endpoints such as `GetUserPhotos`, `GetMostViewedPhotos`, `RecordView`, and `GetUserGroups` were executing Entity Framework Core database operations (like `ToListAsync`, `SingleOrDefaultAsync`, `CountAsync`) and asynchronous channel writes (`WriteAsync`) without accepting or passing a `CancellationToken`. This allowed operations to continue consuming database connections, thread pool threads, and server resources even if the client disconnected or cancelled the request early, potentially leading to resource exhaustion and a Denial-of-Service (DoS) condition.
**Learning:** For endpoints returning large collections of entities (e.g. photos or groups) or modifying states (recording views), always support graceful cancellation to conserve server resources when subjected to unexpected or malicious disconnects.
**Prevention:** Ensure all `[HttpGet]`, `[HttpPost]`, and similar ASP.NET Core endpoints are defined with a `CancellationToken cancellationToken = default` parameter, and consistently thread this token through to all async data access and processing operations.

## 2026-06-25 - [MEDIUM] Fix missing CancellationToken in GetUserGroups, GetUserPhotos, GetMostViewedPhotos, and RecordView
**Vulnerability:** Several endpoints in `AuthController` and `PhotosController` executed long-running Entity Framework Core database operations (`FindAsync`, `ToListAsync`, `CountAsync`) and asynchronous channel writes (`WriteAsync`) without passing a `CancellationToken`. This allowed the operations to continue consuming database connections, memory, and thread pool resources even after a client abruptly disconnected, potentially leading to resource exhaustion and Denial-of-Service (DoS).
**Learning:** For endpoints returning large collections or performing background processing, graceful cancellation is critical to conserve server resources when subjected to unexpected or malicious disconnects.
**Prevention:** Consistently inject a `CancellationToken cancellationToken = default` parameter into ASP.NET Core API controller methods and thread it through to all async data access and processing operations.
## 2024-05-24 - Pagination Limits to Prevent DoS

**Vulnerability:** API endpoints (`GetPhotos`, `GetUserPhotos`, `GetMostViewedPhotos`, `GetAllUsers`, `GetReports`) lacked maximum boundary constraints on parameters like `pageSize` and `count`, allowing malicious actors to request unbounded result sets (e.g., `count=1000000`), causing excessive database load and Out-Of-Memory (OOM) vulnerabilities.

**Learning:** Relying solely on default parameter values (e.g., `count = 10`) is insufficient. Client-provided inputs can override these defaults and must be strictly clamped before executing database operations, particularly those involving `.Skip()` and `.Take()`.

**Prevention:** Consistently apply `Math.Clamp()` and `Math.Max()` to pagination and count variables prior to constructing Entity Framework Core queries. Ensure that rate limiting is accompanied by input length constraints to fully mitigate Denial-of-Service risks.

## 2026-07-22 - Fix Bcrypt Hash Denial of Service (DoS)
**Vulnerability:** The application was vulnerable to Bcrypt Hash Denial of Service (DoS) as DTO objects allowed passwords up to 100 characters in length. Because Bcrypt natively truncates inputs over 72 bytes, exceptionally long strings could be processed unsafely, potentially causing high CPU overhead during hashing and contributing to truncation-related security vulnerabilities.
**Learning:** Relying on generic string lengths (like `[StringLength(100)]`) for password inputs is insufficient when using Bcrypt. Bcrypt has a hard limit of 72 bytes.
**Prevention:** Always enforce a strict maximum length limit of 72 characters (`[StringLength(72)]`) on password fields in DTOs to align with Bcrypt's native truncation limit and prevent potential DoS through excessive string parsing before hashing.
## 2025-02-27 - Fix Entity Framework Core FindAsync Overload Resolution Bug
**Vulnerability:** A minor DoS/crash vector where `FindAsync` incorrectly interpreted `CancellationToken` as a key property because it was passed alongside a single primary key without being wrapped in an object array (e.g. `FindAsync(request.RequestId.Value, cancellationToken)`).
**Learning:** EF Core's `FindAsync(params object[] keyValues)` will consume the `CancellationToken` as a second key parameter if the first parameter is just a single ID, which bypasses the intended `FindAsync(object[] keyValues, CancellationToken cancellationToken)` overload.
**Prevention:** Always wrap the key values in an object array when passing a `CancellationToken`, like this: `await _context.Entity.FindAsync(new object[] { id }, cancellationToken);`.

## 2026-07-23 - Fix missing authorization on tags search endpoint
**Vulnerability:** Missing authentication on the `/api/tags/search` endpoint.
**Learning:** Unprotected API endpoints, even those seemingly innocuous like tag searches, can allow unauthenticated users to enumerate internal data structures. This can lead to information disclosure or be chained into larger attacks by mapping out valid internal parameters.
**Prevention:** Always verify that every controller endpoint exposing application state or data requires authentication (e.g., via the `[Authorize]` attribute) unless explicitly intended for public access.
