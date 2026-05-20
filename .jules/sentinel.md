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

## 2024-05-24 - Fix CodeQL Path Traversal Parsing
 **Vulnerability:** False Positive / CodeQL parsing failure for `fileName != safeFileName`.
 **Learning:** CodeQL flags `safeFileName != fileName` checks as "sensitive action guarded by user-provided value". To satisfy CodeQL while preventing Windows-style payload bypasses on Linux, the input string must be reassigned *after* stripping path components: `fileName = Path.GetFileName(fileName.Replace("\\", "/"));`, followed by standard input validation.
 **Prevention:** Combined strict path normalization `Replace("\\", "/")` with `Path.GetFileName()` reassignment to satisfy both static analysis scanners and secure coding principles.

## 2026-05-24 - Harden Path Traversal Validation in ImagesController
 **Vulnerability:** The previous path traversal check simply validated `fileName != Path.GetFileName(fileName.Replace("\\", "/"))` but CodeQL gets confused if the extraction logic is combined in a single check.
 **Learning:** In .NET on Linux platforms, `Path.GetFileName()` does not treat backslashes (`\`) as directory separators. To prevent path traversal bypasses involving backslashes, always normalize the path by replacing `\` with `/` before calling `Path.GetFileName()`. CodeQL likes an explicit variable reassignment.
 **Prevention:** To prevent Path Traversal (CWE-22) and satisfy CodeQL analysis, explicitly clear taint by computing `safeFileName = Path.GetFileName(input.Replace("\\", "/"))` and checking `input != safeFileName` afterwards.

## 2024-05-24 - [Fix Information Leakage and DoS Risk in Moderation Service]
**Vulnerability:** The Python moderation service lacked file size limits, allowing memory exhaustion via large uploads (DoS). Furthermore, its global exception handler returned raw exception details (`detail=str(e)`), risking Information Leakage of internal paths, library versions, or ML model errors.
**Learning:** Python microservices, especially ML inferencing services, are vulnerable to DoS if they read unbounded streams into memory (`await file.read()`). Additionally, propagating raw exception strings to the client violates the "Fail securely" principle and aids attackers in reconnaissance.
**Prevention:** Always enforce strict file size bounds (e.g. 50MB) before or during file reads in FastAPI. Implement robust exception handling that logs the detailed error internally but returns a safe, generic `HTTPException` message to the client.

## 2024-05-16 - Add StringLength Bounds to DTOs
**Vulnerability:** Found DTOs like ContactRequestDto and ReportDto missing string length bounds. This can lead to a Denial of Service (DoS) attack if large payloads are submitted to fields without length restrictions.
**Learning:** Even simple DTOs need explicit validation using DataAnnotations, such as [StringLength(X)] to prevent malicious actors from sending excessively large strings that consume memory and CPU resources.
**Prevention:** Always add [Required] and [StringLength] data annotations to DTO properties when handling user input.

## 2024-05-24 - [Remove Hardcoded Secrets from Configuration Fallbacks]
**Vulnerability:** The application fell back to hardcoded secrets for `Jwt:Key` and the database `connectionString` if they were missing from the configuration. This could lead to production environments running with known, insecure defaults, allowing for token forgery and potential database compromise.
**Learning:** Providing "helpful" defaults for sensitive configuration variables in code creates a critical risk if a deployment misses a configuration step, as the application will start without errors but with compromised security.
**Prevention:** Fail fast and securely. Always throw configuration exceptions at startup if critical secrets or connection strings are missing, rather than falling back to hardcoded values. In tests, use explicit setting injection via WebApplicationFactory so the secrets are not mistakenly added to base JSON configs.
## 2024-05-18 - [Add StringLength Bounds to DTOs]
**Vulnerability:** Found DTOs like `UserRegisterDto`, `UserLoginDto`, `CreateGroupRequest`, `CreateInvitationDto`, and `RoleUpdateDto` missing string length bounds. This can lead to a Denial of Service (DoS) attack if large payloads are submitted to fields without length restrictions.
**Learning:** Even simple DTOs need explicit validation using DataAnnotations, such as `[StringLength(X)]` to prevent malicious actors from sending excessively large strings that consume memory and CPU resources.
**Prevention:** Always add `[Required]` and `[StringLength]` data annotations to DTO properties when handling user input.
## 2024-05-20 - Add Rate Limiting for Report Endpoint
**Vulnerability:** The `/api/photos/{id}/report` endpoint was missing rate limiting, making it vulnerable to Denial of Service (DoS) attacks and spam reporting.
**Learning:** Endpoints that trigger database writes and external notifications must be protected with rate limiting to prevent abuse.
**Prevention:** Always add `[EnableRateLimiting]` to endpoints handling user-submitted content or actions like reporting, and ensure the rate limit policy is defined in `Program.cs`. When configuring rate limits for authenticated endpoints, partition by the user's ID rather than their IP address to avoid penalizing users behind the same NAT.
