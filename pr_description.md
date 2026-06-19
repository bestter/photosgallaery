🚨 Severity: High
💡 Vulnerability: User Enumeration (Timing Attack)
🎯 Impact: Attackers could determine if an email or username was already registered by measuring the time the `/api/auth/register` endpoint took to respond. Duplicate registrations returned immediately, while new registrations were delayed by the computationally expensive password hashing process.
🔧 Fix: Added a dummy password hash computation (`BCrypt.Net.BCrypt.HashPassword(request.Password)`) when returning the masked success response for existing users. This equalizes the CPU processing time regardless of whether the user exists or not, effectively blinding timing-based enumeration attacks.
✅ Verification: Ran `dotnet test PhotoAppApi.Tests/ --filter AuthControllerTests` to ensure existing enumeration protection tests (e.g., `Register_ExistingUser_ReturnsOkToPreventEnumeration`) still pass and that the dummy hashing does not introduce functional regressions.
