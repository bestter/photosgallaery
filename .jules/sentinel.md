## 2023-10-27 - Unbounded Memory Read in file uploads
**Vulnerability:** Calling `await file.read()` in FastAPI/Starlette loads the entire file into memory, causing a high risk of Out-Of-Memory (OOM) Denial-of-Service attacks when malicious actors upload extremely large files or use decompression bombs.
**Learning:** File size limits must be checked dynamically while reading the file in chunks rather than relying on content-length headers or reading the entire file first.
**Prevention:** Always use chunked reading loops (e.g., `while True: chunk = await file.read(1MB)`) when processing file uploads, verifying the cumulative size does not exceed the allowed threshold before proceeding.
