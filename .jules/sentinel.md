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

## $(date +%Y-%m-%d) - Path Traversal Vulnerability in ImagesController

**Vulnerability:** The `GetImage` and `GetThumbnail` endpoints in `ImagesController.cs` solely relied on `Path.GetFileName()` to sanitize the `fileName` route parameter. On Linux/Unix environments, `Path.GetFileName()` does not remove Windows-style path separators (`\`), allowing potential path traversal payloads (e.g., `..\..\etc\passwd`) to bypass the intended security check and resolve outside the application's root directory.

**Learning:** `Path.GetFileName()` is highly dependent on the underlying OS. When a .NET application runs on Linux, it only treats `/` as a directory separator. Attackers can exploit this mismatch by supplying `\` characters, which the application fails to recognize as traversal attempts. Additionally, when testing file uploads that rely on libraries like `ImageSharp`, dummy text files will fail to parse and can cause tests to throw unhandled exceptions instead of returning standard HTTP error codes like `BadRequest`.

**Prevention:** Never rely purely on `Path.GetFileName()` or `Path.GetInvalidFileNameChars()` for cross-platform security validation of user-supplied paths. Explicitly reject file paths containing `/`, `\`, or `..` before attempting any file resolution. When writing unit tests for upload functionality, always mock file streams with real image bytes (e.g., a minimal 1x1 GIF) to prevent internal library exceptions from masking the behavior of the controller.
