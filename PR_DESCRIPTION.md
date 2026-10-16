## 🧪 [testing improvement description]
**Title:** "🧪 [Test] Add unit tests for load_image function"

### Description:
* 🎯 **What:** This PR addresses the missing test coverage for the `load_image` function in `main.py`. This function converts raw byte data into a `PIL.Image` in "RGB" format.
* 📊 **Coverage:** The new tests cover:
  - Happy path (`test_load_image_success`): verifying that a valid byte stream returns an instance of `PIL.Image` with the expected format ("RGB") and dimensions.
  - Error condition (`test_load_image_invalid_data`): validating that passing invalid byte arrays raises an `UnidentifiedImageError` handled by `PIL`.
* ✨ **Result:** The test suite now passes with 13 tests, eliminating an untested piece of logic that could have potentially broken runtime execution.
