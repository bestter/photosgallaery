## 2025-04-26 - [Defense in Depth: Protecting Against Poisoned Database Records]
**Vulnerability:** Path Traversal via database-sourced fields (CWE-22). The application correctly sanitized user-uploaded filenames but failed to re-sanitize those filenames when reading them back from the database (e.g., in `photo.FileName`) before using them in file operations like `Path.Combine()`.Collapse commentComment on line L2bestter commented on Apr 30, 2026 bestteron Apr 30, 2026OwnerAuthorMore actionsPut back all sentinel documentation!ReactWrite a replyResolve comment
**Learning:** Even if data is sanitized upon input (e.g., file upload), you cannot implicitly trust data coming from the database. A malicious actor with direct DB access or who exploits another vulnerability (like SQLi) could poison the `FileName` column (e.g., `../../../etc/passwd`), leading to arbitrary file read/write/delete during maintenance or deletion tasks.
**Prevention:** Implement Defense in Depth. Always re-sanitize fields that dictate file paths using functions like `Path.GetFileName()` right before they are used in file system APIs (`System.IO.File.Delete`, `System.IO.File.Move`, `System.IO.File.OpenRead`), regardless of their origin.
## 2026-04-27 - [Defense in Depth: Protecting Against Poisoned Database Records]
**Vulnerability:** Path Traversal via database-sourced fields (CWE-22) in Background Service. The application correctly sanitized user-uploaded filenames but failed to re-sanitize those filenames when reading them back from the database (e.g., in `photo.FileName`) before using them in file operations like `Path.Combine()` in `HashCalculationBackgroundService.cs`.
**Learning:** Even if data is sanitized upon input (e.g., file upload), you cannot implicitly trust data coming from the database. A malicious actor with direct DB access or who exploits another vulnerability (like SQLi) could poison the `FileName` column (e.g., `../../../etc/passwd`), leading to arbitrary file read/write during background processing tasks.
**Prevention:** Implement Defense in Depth. Always re-sanitize fields that dictate file paths using functions like `Path.GetFileName()` right before they are used in file system APIs, regardless of their origin.
## 2024-05-24 - [Username Enumeration via Login Error Messages]
**Vulnerability:** Username Enumeration. The login endpoint (`/api/auth/login`) returned different HTTP status codes and error messages depending on whether the username was found (`401 Unauthorized("Identifiants incorrects.")`) or the password was incorrect (`400 BadRequest({ message: "Mot de passe incorrect." })`).
**Learning:** Returning specific error messages during authentication allows an attacker to enumerate valid usernames. If an attacker knows a username is valid, they can focus their brute-force or credential stuffing attacks on that specific account.
**Prevention:** Always return a generic error message (e.g., "Identifiants incorrects.") and the same HTTP status code (e.g., `401 Unauthorized`) for all authentication failures (invalid username, invalid password) to prevent information leakage.

## 2024-05-27 - [Failed Data Deletion: Hardcoded Public Path on Delete]
**Vulnerability:** Failed Data Deletion (Orphaned Files). The `DeletePhoto` method in `PhotosController.cs` was using a hardcoded `wwwroot/images` path to delete photos and didn't attempt to delete thumbnails, while uploads and other features used the `PrivateImages` directory.
**Learning:** If file storage paths are updated for features like uploads or viewing (e.g., migrating from public `wwwroot` to a private `PrivateImages` folder), all related operations like deletions must also be updated. Otherwise, sensitive data will be left orphaned on the filesystem, accessible if the directory is ever exposed, and causing resource exhaustion.
**Prevention:** Use consistent path resolution across all file operations. Centralize path configuration (e.g., using `_env.ContentRootPath` consistently) and ensure deletion logic covers all generated assets (like thumbnails).
## 2025-05-01 - [Data Exposure IDOR on Collection Endpoints]
**Vulnerability:** Insecure Direct Object Reference (IDOR) / Data Exposure. Endpoints like `/api/photos/user/{username}`, `/api/photos/user/{username}/likes`, and `/api/photos/most-viewed` returned collections of photos without applying row-level authorization checks. This allowed any user (or unauthenticated visitor) to retrieve metadata (and IDs) of photos belonging to private groups.
**Learning:** When fetching collections of records (e.g., photos), row-level security must be enforced in the query itself to filter out records the current user is not authorized to view. It's not enough to secure the image file endpoint; the metadata endpoints must also be restricted. Group-based access control needs to be consistently applied across all endpoints that return lists of objects.
**Prevention:** Always apply the authorization filter logic (e.g., `GroupId == null || UserGroups.Contains(GroupId)`) when building IQueryable LINQ queries for collections that contain mixed visibility items, ensuring the database only returns authorized records to the caller.
## 2026-04-30 - [Insecure Default JWT Secret Key]
**Vulnerability:** Placeholder JWT Secret in Configuration. The application used a known, weak placeholder string as the JWT signing key in `appsettings.json`.
**Learning:** Hardcoding or using well-known placeholder secrets in configuration files is a major security risk. If an application is deployed with these defaults, attackers can easily forge JWT tokens, bypassing authentication and potentially gaining administrative access.
**Prevention:** Never include real secrets in source control. Use empty strings or distinct placeholders for development, and implement mandatory validation at application startup to ensure a secure, unique key is provided (e.g., via environment variables) before the application can start.
## 2026-04-30 - Prevent Default Database Credentials Deployment
**Vulnerability:** Placeholder database credentials ("YOUR_DB_SERVER", etc.) left in `appsettings.json` can cause broken connections if deployed, or potentially unauthorized access if a default service is exposed.
**Learning:** Emptying connection strings in configuration files is insufficient if it causes obscure errors at startup (e.g., when `ServerVersion.AutoDetect` fails).
**Prevention:** Explicitly validate connection strings in `Program.cs` during startup. If null, empty, or containing placeholder text, fail fast with a clear, localized `InvalidOperationException` to guide developers to correctly configure the environment.
## 2024-05-01 - Path Traversal in GetImage and GetThumbnail
**Vulnerability:** Path Traversal via `fileName` parameter using backslashes (`\`) which evade `Path.GetFileName()` on Linux.
**Learning:** `Path.GetFileName(@"..\..\etc\passwd")` on Windows correctly returns `"passwd"`, effectively neutralizing traversal. However, on Linux systems, backslashes are valid filename characters, so it returns the entire malicious string `@"..\..\etc\passwd"`. If this string is later appended to a directory path, it can act as a valid filename on Linux but will be treated as directory traversal if the resulting path is consumed by another library or if it is ever parsed on a Windows client/system later.
**Prevention:** To prevent Path Traversal (CWE-22) across different OS platforms, do not rely solely on `Path.GetFileName()` as it does not strip Windows-style separators (`\`) on Linux. Always validate the final combined path using `Path.GetFullPath()` and ensure it `StartsWith()` the expected absolute base directory, and explicitly check filenames for `..` sequences.

## 2025-02-28 - Path Traversal (CWE-22) Remediation
**Vulnerability:** Path Traversal (CWE-22) in `ImagesController.cs`. Client-provided `fileName` parameter was passed to file system APIs relying solely on `Path.GetFileName()`, which is not sufficient against Windows-style payloads when running on a Linux host.
**Learning:** `Path.GetFileName("..\\..\\etc\\passwd")` returns `..\\..\\etc\\passwd` on Linux. Thus, cross-OS payloads bypass simplistic `GetFileName` protection.
**Prevention:**
1. Reject explicit directory traversal characters (`/`, `\`, `..`) in user-supplied strings directly before they touch any file API.
2. For paths read from a database or not directly supplied via route parameter, normalize the path via `fileName.Replace("\\", "/")` prior to evaluating `Path.GetFileName()` to ensure cross-OS compatibility and prevent bypasses.

## 2026-05-01 - Add tests for AuthController.Login
**Vulnerability:** Lack of test coverage for the core `Login` endpoint.
**Learning:** Implementing tests ensures the application reacts correctly to valid credentials, invalid credentials, and forbidden roles, verifying key application logic and status codes (200, 401, 403, 500).
**Prevention:** Complete unit test coverage using in-memory databases and mocked JWT configurations guarantees authentication responses are verified to avoid future regressions.
## 2025-02-14 - Prevent Username Enumeration via Timing Attack
**Vulnerability:** The `/api/auth/login` endpoint would return instantly if a user was not found, but took significantly longer to respond if the user existed because of the `BCrypt.Verify` operation. An attacker could observe this timing difference to enumerate valid usernames in the database.
**Learning:** Using computationally expensive hashing algorithms like BCrypt means the password validation branch is slow. Bailing early when the user is not found exposes the timing difference.
**Prevention:** Always verify the password against a dummy hash when the user doesn't exist to ensure the execution time of the authentication request remains constant regardless of whether the username is valid or not.

## 2026-05-04 - Path Traversal Vulnerability in ImagesController

**Vulnerability:** The `GetImage` and `GetThumbnail` endpoints in `ImagesController.cs` solely relied on `Path.GetFileName()` to sanitize the `fileName` route parameter. On Linux/Unix environments, `Path.GetFileName()` does not remove Windows-style path separators (`\`), allowing potential path traversal payloads (e.g., `..\..\etc\passwd`) to bypass the intended security check and resolve outside the application's root directory.

**Learning:** `Path.GetFileName()` is highly dependent on the underlying OS. When a .NET application runs on Linux, it only treats `/` as a directory separator. Attackers can exploit this mismatch by supplying `\` characters, which the application fails to recognize as traversal attempts. Additionally, when testing file uploads that rely on libraries like `ImageSharp`, dummy text files will fail to parse and can cause tests to throw unhandled exceptions instead of returning standard HTTP error codes like `BadRequest`.

**Prevention:** Never rely purely on `Path.GetFileName()` or `Path.GetInvalidFileNameChars()` for cross-platform security validation of user-supplied paths. Explicitly reject file paths containing `/`, `\`, or `..` before attempting any file resolution. When writing unit tests for upload functionality, always mock file streams with real image bytes (e.g., a minimal 1x1 GIF) to prevent internal library exceptions from masking the behavior of the controller.

## 2026-05-04 - Authorization Bypass via Null Equivalence in Ownership Check

**Vulnerability:** In `PhotosController.DeletePhoto`, the authorization logic checked ownership using `if (photo.UploaderUsername != currentUsername && !isAdmin)`. Since `photo.UploaderUsername` is nullable (e.g., photos uploaded by anonymous users or before users were forced to have names) and `currentUsername` could be null (if the JWT token lacks the Name claim or the user isn't fully authenticated), `null != null` evaluates to `false`. This caused the `Forbid()` block to be skipped, allowing an anonymous user to delete photos lacking an uploader username.

**Learning:** When comparing two values for ownership or authorization—especially strings that can be null—do not assume that equality (or equivalence through double-nulls) implies valid authorization. Null equivalence can inadvertently bypass security checks if neither the resource nor the actor has a valid identifier.

**Prevention:** Always explicitly verify that the acting user has a valid, non-empty identifier before comparing it against the resource's owner identifier. For example, explicitly check `string.IsNullOrEmpty(currentUsername)` and reject the action if it's true, regardless of what the resource's owner identifier is.
## 2026-05-05 - [Fixing IDOR Vulnerability in Modification Endpoints]
**Vulnerability:** Insecure Direct Object Reference (IDOR) on modifying actions. Actions such as `ToggleLike` and `ReportPhoto` only validated user authentication without verifying the user's specific group-based authorization rights to view and interact with the private photo.
**Learning:** Developers correctly secured data fetching APIs like `GetPhotos` with precise group filtering and roles matching (`isAdmin`). But endpoints designed for modifying specific object instances (e.g. Likes and Reports) did not re-validate the target photo’s authorization context.
**Prevention:** It is mandatory to enforce authorization and ownership context validations comprehensively across ALL interaction actions, including object modifications, rather than isolating permissions logic only on data retrieval.

## 2024-05-18 - Prevent Denial of Service (DoS) via File Upload Exhaustion
**Vulnerability:** File upload endpoints (like `/api/photos/upload`) were lacking explicit rate limiting, making the application susceptible to resource exhaustion or DoS attacks from automated scripts repeatedly uploading large payloads.
**Learning:** Even if the file size itself is limited (`[RequestSizeLimit]`), an attacker could still exhaust server resources (CPU, I/O, disk space) by sending many requests in a short amount of time.
**Prevention:** Implement endpoint-specific rate limiting (`EnableRateLimiting`) partitioned by IP address on resource-intensive endpoints (such as file uploads) to constrain the maximum number of requests a single user can make within a specified time window.

## 2024-05-07 - [Missing Role Authorization on Maintenance Endpoint]
**Vulnerability:** The endpoint `/api/photos/maintenance/backfill-hashes` had a `[Authorize]` attribute, allowing any authenticated user to trigger an expensive backfill operation meant only for Admins.
**Learning:** For maintenance and administrative endpoints, simple authentication is insufficient. If a role requirement (e.g. `[Authorize(Roles = "Admin")]`) is missing, it results in a Broken Access Control / Missing Authorization vulnerability, which could be abused for Denial of Service or unintended data modification.
**Prevention:** Always verify that administrative routes explicitly enforce role-based access control, rather than just requiring an authenticated session.

## 2026-05-07 - [Path Traversal in ImagesController]
**Vulnerability:** Path Traversal (CWE-22) in GetImage and GetThumbnail endpoints.
**Learning:** Relying on sanitization (like Path.GetFileName) alone can be bypassable or leave edge cases. A "reject-early" strategy for route parameters is more secure.
**Prevention:** Strictly validate input strings that will be used in path construction. Reject any input containing directory separators ('/', '\') or traversal sequences ('..') before any further processing.
## 2025-05-08 - Added tests for ContactController.SubmitContactForm
**Vulnerability:** Lack of test coverage for the ContactController endpoint.
**Learning:** Implementing tests ensures the application correctly handles requests, validates input fields, correctly utilizes the mocked email service and returns the expected HTTP responses.
**Prevention:** By verifying valid, missing fields, and error states, we ensure stability and protect against potential unhandled exceptions or regressions in external dependency calls.

## 2024-05-08 - Added Rate Limiting to Registration Endpoint to Prevent CPU Exhaustion
**Vulnerability:** Unauthenticated registration endpoint (`/api/auth/register`) performed expensive BCrypt hashing without rate limiting, exposing the application to Denial of Service (DoS) via CPU exhaustion.
**Learning:** Endpoints that consume significant CPU resources (like password hashing) must be protected against abuse, especially when they are unauthenticated.
**Prevention:** Apply a strict fixed-window rate limiter (e.g., 3 requests per 10 minutes per IP) using `[EnableRateLimiting]` on resource-intensive endpoints.
## 2024-05-18 - [Fix Path Traversal in ImagesController]
 **Vulnerability:** Path Traversal (CWE-22) in `ImagesController.cs` methods `GetImage` and `GetThumbnail` where the path was only sanitized using `Replace("\\", "/")` and `.Contains("..")`, missing other evasive techniques and edge cases across platforms.
 **Learning:** Simple string replacement and `Contains("..")` are insufficient for robust path traversal prevention. CodeQL and secure coding guidelines recommend a consolidated validation utilizing `Path.GetFileName` to ensure the input resolves strictly to a simple filename, without any directories or invalid characters.
 **Prevention:** Implement a consolidated guard clause: `if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName != Path.GetFileName(fileName.Replace("\\", "/"))) return BadRequest("Invalid file name.");`
## 2024-05-18 - ContactControllerTests Missing Tests Fix
 **Learning:** When adding tests for `null` requests or explicit whitespace string requests (`" "`), xUnit will issue warning `xUnit1012` if `null` is used for a string parameter via `[InlineData(null, ...)]` when `string` is non-nullable. It still compiles and tests pass, but generates warnings.
 **Action:** Handled null requests separately using `SubmitContactForm_NullRequest_ReturnsBadRequest` without `InlineData` parameter injection, and passed explicit strings `" "` via `InlineData` for whitespace validation coverage.

## 2024-05-24 - [Rate Limiting Unauthenticated Endpoints]
**Vulnerability:** The `/api/Contact` endpoint for submitting contact forms was completely unauthenticated and lacked any rate limiting. This allowed malicious actors to abuse the endpoint, leading to spam (triggering unbounded email dispatch) and potential Denial of Service (DoS) by exhausting third-party service quotas or CPU resources.
**Learning:** Endpoints like login and registration were previously secured with ASP.NET Core's rate limiting, but peripheral unauthenticated endpoints (like a contact form) are easily missed during threat modeling, leaving the application open to resource exhaustion attacks.
**Prevention:** Always enforce strict rate limiting policies (partitioned by IP) on ALL public-facing, unauthenticated endpoints that perform work (like database writes, email dispatches, or heavy computations). When adding new endpoints that don't require `[Authorize]`, mandate a rate limiting attribute (`[EnableRateLimiting("PolicyName")]`) by default.

## 2025-05-10 - [Defense in Depth via Security Headers]
**Vulnerability:** Missing security headers like Content-Security-Policy (CSP), X-Content-Type-Options, Referrer-Policy and Permissions-Policy in the backend allowed potential risk of XSS and framing-related attacks.
**Learning:** Adding defense-in-depth via HTTP security headers is a crucial measure even if the application logic itself relies on escaping in modern frameworks (e.g. React). Expanding the backend middleware inside Program.cs effectively mitigates content-based vulnerabilities at the delivery level.
**Prevention:** Always verify that a comprehensive set of HTTP security headers (e.g., CSP, nosniff, referrer-policy) are configured by default in Program.cs.
## 2026-05-24 - File Upload Vulnerabilities: MIME spoofing, EXIF injection, Polyglot files
**Vulnerability:** The `UploadPhotos` endpoint trusted the client-provided `IFormFile.ContentType` and file extension, and simply copied the stream to S3. This allowed attackers to upload polyglot files or files with dangerous EXIF payloads (webshells).
**Learning:** Never trust client input for file uploads. File extensions and Content-Type headers can be trivially spoofed. Saving files directly without re-encoding preserves embedded metadata which can contain executable payloads or tracking information.
**Prevention:** Implement Defense in Depth: 1. Validate "Magic Bytes" (file signatures) to confirm the actual file type. 2. Discard original filenames and use generated UUIDs. 3. Re-encode the image using a library like ImageSharp and explicitly strip all EXIF/IPTC/XMP metadata before saving.

## 2024-05-24 - [Add Rate Limiting to Anonymous View Endpoint]
**Vulnerability:** The unauthenticated endpoint `[HttpPost("{id}/view")]` was missing rate limiting. This could allow an attacker to flood the endpoint, causing excessive events to be written to the channel and potentially causing Denial of Service (DoS) by exhausting memory or CPU.
**Learning:** Even fast, asynchronous endpoints that write to a channel need protection if they are publicly accessible, to prevent the channel from being overwhelmed or filled with spam.
**Prevention:** Always enforce rate limiting (e.g., `[EnableRateLimiting("ViewLimiter")]`) on unauthenticated endpoints, and configure the limits appropriately in `Program.cs` based on IP partitions.

## 2024-05-24 - [Rate Limiting and Input Bounding for Group Requests]
**Vulnerability:** The unauthenticated (or broadly available) `SubmitGroupRequest` endpoint in `GroupRequestsController` lacked both rate limiting and input length validation on properties like `Name` and `Description`. This exposed the application to Denial of Service (DoS) and potential database/memory exhaustion from large payloads or rapid submission spam.
**Learning:** Endpoints that allow users to submit unconstrained text into the database can be targeted to consume significant storage and network resources. Similarly, endpoints that write to the database require rate limiting to prevent spam and resource exhaustion.
**Prevention:** Apply rate limiting (`[EnableRateLimiting]`) partitioned by IP and enforce sensible string length boundaries (e.g. `[StringLength(100)]`) on DTO properties that map to database columns.

## 2024-05-18 - [Rate Limiting and Length Validating Database Search Queries]
**Vulnerability:** Unauthenticated or broadly available database search queries, like auto-completing tags, were exposed without rate limiting or input bounds. This could allow attackers to send a massive volume of requests or send very long string lengths that cause complex, resource-heavy SQL `LIKE` query evaluations, creating a risk for Denial of Service (DoS) and database exhaustion.
**Learning:** Database operations on search endpoints are computationally expensive. Leaving them unrestricted invites abuse that can slow down or crash the backend database, impacting availability.
**Prevention:** Apply specific rate limiting policies (e.g. `[EnableRateLimiting("TagsLimiter")]`) to constrain the maximum number of requests a single user can make. Additionally, strictly bound the input query length (e.g. `if (q.Length > 50)`) before querying the database to limit computation intensity.

## 2024-10-24 - [Rate Limiting and Input Bounding for Invitations]
**Vulnerability:** The `CreateInvitation` endpoint was protected by `[Authorize]` but lacked rate limiting. It triggered external action (`_emailService.SendInvitationEmailAsync`), which could be abused by an authenticated user to perform spamming or Denial of Service by exhausting email quota.
**Learning:** Endpoints that trigger external services or consume significant resources should have explicit rate limit policies, even if they are authenticated.
**Prevention:** Always add rate limiting using the `[EnableRateLimiting("InviteLimiter")]` attribute for endpoints performing resource-intensive operations, such as sending emails, and define policies mapped to IP addresses in `Program.cs`.
## 2024-05-24 - Fix Path Traversal in ImagesController
 **Vulnerability:** Path Traversal (CWE-22) in `ImagesController.GetImage` and `ImagesController.GetThumbnail`.
 **Learning:** The previous path validation (`fileName.Contains("..")`) and CodeQL fix `Path.GetFileName(fileName)` were insufficient because `.NET` on Linux doesn't treat backslashes as path separators, allowing traversal bypasses. Also, simple truncation without validation (`fileName = Path.GetFileName(fileName)`) implicitly trusts invalid inputs.
 **Prevention:** Replaced implicit truncation with strict validation. Replaced `\` with `/` to ensure cross-platform safety, extracted the pure file name, and explicitly rejected the request if the extracted name doesn't match the input exactly (`safeFileName != fileName`). Added redundant checks for `..` and invalid characters for defense-in-depth.
