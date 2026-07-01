#!/bin/bash
# Check memory rules for Thread Pool Starvation from DbContext Task.WhenAll
# Rule: "DbContext is not thread-safe. Executing multiple asynchronous Entity Framework Core queries concurrently on the same context instance using Task.WhenAll (e.g., await Task.WhenAll(query1, query2)) can cause internal concurrency conflicts, state corruption, and InvalidOperationException... Always await EF Core IQueryable operations sequentially (e.g., .ToListAsync()) to ensure the DbContext and its underlying connection are used safely by a single thread at a time."
