🚨 Severity: LOW
💡 Vulnerability: User Enumeration
🎯 Impact: Attackers could determine if an email address exists in the system or is a member of a group.
🔧 Fix: Verified that the fix is already implemented in `InvitationsController.cs`. A generic response message is used for all code paths, and sending emails has been offloaded to a background task using `Task.Run` and `IServiceScopeFactory`, mitigating side-channel timing leaks.
✅ Verification: Ran existing test suite, verified background processing pattern, and confirmed the standardized generic message response exists in the code logic.
