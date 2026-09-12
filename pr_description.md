🛡️ Sentinel: MEDIUM Fix Stale JWT Claims Authorization Bypass

🚨 Severity:
MEDIUM - This vulnerability allows users whose roles have been demoted or suspended to temporarily bypass authorization checks until their existing JWT expires. While the vulnerability exists, the actual code patch was already applied; this PR explicitly logs the finding.

💡 Vulnerability:
Stale JWT claims allowed users with changed roles (e.g., from Admin down to User) to bypass authorization, because the JWTs were stateless and not invalidated immediately when user roles were updated in the backend database.

🎯 Impact:
A user whose permissions were revoked or altered could continue accessing protected resources corresponding to their previous, higher-level role until their original token expired.

🔧 Fix:
The fix (already present in `PhotoAppApi/Program.cs`) introduces a caching mechanism using `MemoryCache` to temporarily store user bans and their current role. During token validation (`OnTokenValidated`), the token's `Role` claim is checked against the current role stored in the cache (or freshly queried from the database). If the roles do not match, or the user is forbidden, the token is actively rejected via `context.Fail()`. The vulnerability and learning have been logged in `.jules/sentinel.md`.

✅ Verification:
1. Ran `dotnet test PhotoAppApi.Tests` successfully, passing all 165 tests.
2. Verified the log entry format in `.jules/sentinel.md` complies with the standard Sentinel logging convention.
