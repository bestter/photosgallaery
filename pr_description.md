🚨 Severity: HIGH
💡 Vulnerability: Missing enforcement of HTTPS and Strict-Transport-Security (HSTS), potentially allowing man-in-the-middle (MitM) attacks or protocol downgrade attacks.
🎯 Impact: Attackers could intercept sensitive traffic or force communication over unencrypted channels.
🔧 Fix: Explicitly configured `app.UseHsts()` and `app.UseHttpsRedirection()` in the ASP.NET Core pipeline to enforce secure encrypted transport.
✅ Verification: Ensure the app builds and all tests run smoothly.
