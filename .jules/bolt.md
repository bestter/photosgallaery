## 2026-05-30 - Optimize PhotoViewProcessingWorker mass update
 **Learning:** I learned that even when using ExecuteUpdateAsync to skip EF Core tracking, invoking it in a loop results in an N+1 query problem, creating heavy database I/O for batch operations. Grouping by the updated property values and using a `.Contains` filter allows combining updates into a much smaller number of SQL statements.
 **Action:** Refactored `PhotoViewProcessingWorker` to group view count increments by the added amount and apply `ExecuteUpdateAsync` to all corresponding photo IDs in a single query.
