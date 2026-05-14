## 2024-05-18 - PhotosController Upload Moderation
**Learning:** Sequential async operations within a foreach loop, like awaiting an external moderation service for multiple images one-by-one, can severely bottleneck throughput as execution times stack additively.
**Action:** Transformed the synchronous `foreach` with embedded `await` calls into a concurrent execution model using LINQ `Select` to build a list of tasks and `Task.WhenAll()` to execute them in parallel, reducing overall wait time significantly.
