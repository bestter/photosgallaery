## 2024-04-24 - [Path Traversal in File Upload]
**Vulnerability:** Path Traversal via unsanitized `file.FileName` used in `Path.Combine` during image upload. This can lead to arbitrary file creation/overwrite if an attacker manipulates the file name in a multipart form-data request.
**Learning:** `Path.Combine` doesn't sanitize directory traversal characters (`../`). Appending an untrusted filename, even when prefixed with a GUID (e.g. `Guid.NewGuid().ToString() + "_" + file.FileName`), allows breaking out of the intended uploads directory if the filename starts with multiple `../`.
**Prevention:** Always sanitize filenames from user input using `Path.GetFileName()` or equivalent before appending to paths.
