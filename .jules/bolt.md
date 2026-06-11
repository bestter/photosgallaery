## 2024-03-24 - Cryptographic Hashing Allocation Overhead
**Learning:** Instantiating `SHA512.Create()` and disposing it in a loop creates significant allocation overhead and slows down hashing, even with fast streams or arrays.
**Action:** Always prefer the static helper methods introduced in newer .NET versions, like `SHA512.HashDataAsync()`, which avoid allocating the provider instance and manage internal pooling efficiently.
