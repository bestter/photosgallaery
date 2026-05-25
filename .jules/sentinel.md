## 2026-05-25 - [Enforce HSTS and HTTPS Redirection]
**Vulnerability:** Missing enforcement of HTTPS and Strict-Transport-Security (HSTS), potentially allowing man-in-the-middle (MitM) attacks or protocol downgrade attacks.
**Learning:** The application was missing explicit configuration to enforce HTTPS communication, relying solely on deployment environment configuration rather than application-level enforcement.
**Prevention:** Explicitly configuring `app.UseHsts()` and `app.UseHttpsRedirection()` in the ASP.NET Core pipeline ensures that the application enforces encrypted transport securely.
