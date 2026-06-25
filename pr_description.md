💡 What: Replaced sequential `DeleteObjectAsync` calls in a foreach loop with batched `DeleteObjectsAsync` requests, accumulating up to 1000 keys per request.

🎯 Why: The original implementation caused a severe N+1 network latency problem where each element to be deleted resulted in a separate HTTP request over the network. Batching requests drastically cuts down the time required.

📊 Impact: Resolves a critical performance issue during garbage collection of Data Protection XML keys.

🔬 Measurement: I created a mock S3 benchmark program that simulated 50ms of network latency per request. Deleting 100 keys sequentially took 5358 ms, while the batched deletion took just 57 ms - an improvement of ~100x.
