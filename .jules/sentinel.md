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
