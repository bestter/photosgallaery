#!/bin/bash
# Reviewer suggested: "Blocking: You must verify the nature of `GetPresignedUrlAsync`. If it makes an external network/DB call, `Environment.ProcessorCount` will artificially bottleneck your API response time and must be increased or reverted to a `Task.WhenAll` approach (without `Task.Run`)."
# In AWS SDK, GetPreSignedURLAsync does not make a network call (it's purely local CPU work for HMAC-SHA256 signature generation).
# Therefore `Environment.ProcessorCount` is correct because it's CPU-bound.
