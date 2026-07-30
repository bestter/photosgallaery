🧪 [testing improvement description]

🎯 What: Added unit tests for edge case pagination parameters (e.g., negative page, extremely large pageSize) in the `GetPhotos` endpoint of `PhotosController` to verify data validity boundary handling (`Math.Clamp` and `Math.Max`).

📊 Coverage:
* Covered the handling of negative pagination `page` parameter bounds check.
* Covered the clamping of excessively large `pageSize` parameter to ensure server load control.
* Verified proper application of pagination limits on database return sizes (via mocked setup) and `X-Total-Count` header behavior.

✨ Result: Ensured that the application correctly prevents Out-Of-Memory/DoS situations and gracefully falls back to sensible defaults when supplied with invalid API pagination inputs. Coverage inside `PhotosControllerTests.cs` has been improved.
