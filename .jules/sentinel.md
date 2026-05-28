## 2025-02-27 - Fix Path Traversal in Image Delivery
**Vulnerability:** The application used an insecure `.StartsWith()` check to validate if a generated file path resided within the `PrivateImages` directory. This check can be bypassed, specifically on different OSes depending on trailing slash behavior or directory substring match (e.g., `PrivateImagesOther`). Also, using the unvalidated string (`filePath`) directly in `FileStream` instead of the normalized one (`fullFilePath`) creates a TOCTOU parsing differential vector.
**Learning:** In ASP.NET Core, `Path.GetDirectoryName(fullFilePath) == expectedDir` is universally safer than string prefixes for directory confinement. Always pass the exact normalized path that passed validation directly to the file stream to avoid parsing loopholes.
**Prevention:** Strictly enforce `string.Equals(Path.GetDirectoryName(fullFilePath), expectedRootPath, StringComparison.OrdinalIgnoreCase)` when validating normalized absolute paths, and ensure the `FileStream` exclusively uses this exact `fullFilePath`.

## 2024-05-22 - [HIGH] Fix hardcoded localhost URL in invitation email
**Vulnerability:** Invitation emails were sent with a hardcoded `http://localhost:5173` URL for the frontend application link, which broken the flow in production and posed a URL confusion risk if a user was tricked or an attacker registered a related domain name, and bypassed the configuration defined in the application environment setup.
**Learning:** Hardcoded environment-specific URLs bypass application configuration (`appsettings.json` and environmental variables), leading to broken functionality in production and potential security confusion when users expect the production domain. Configuration was available in the `Program.cs` startup but was omitted in this controller.
**Prevention:** Always inject `IConfiguration` to retrieve application-wide configurations (such as URLs and keys) instead of hardcoding hostnames.

## 2025-02-27 - Fix User Enumeration Vulnerability in Registration
**Vulnerability:** The `Register` endpoint explicitly returned an error indicating whether an account with a given username or email already existed, allowing an attacker to iterate and enumerate valid user accounts.
**Learning:** Explicit error messages on account creation or recovery flows expose system state and can be leveraged by attackers for enumeration and subsequent credential stuffing or phishing attacks.
**Prevention:** To prevent User Enumeration vulnerabilities in authentication endpoints (like user registration or login), do not return explicit error messages indicating that a username or email is already in use. Instead, silently log the duplicate attempt and return a generic success response to mask account existence.
## 2026-05-25 - [Enforce HSTS and HTTPS Redirection]
**Vulnerability:** Missing enforcement of HTTPS and Strict-Transport-Security (HSTS), potentially allowing man-in-the-middle (MitM) attacks or protocol downgrade attacks.
**Learning:** The application was missing explicit configuration to enforce HTTPS communication, relying solely on deployment environment configuration rather than application-level enforcement.
**Prevention:** Explicitly configuring `app.UseHsts()` and `app.UseHttpsRedirection()` in the ASP.NET Core pipeline ensures that the application enforces encrypted transport securely.

## 2025-02-18 - Fix Email Spoofing Vulnerability in Contact Form
**Vulnerability:** Contact forms setting the `From` email address to user-provided input causing SPF/DKIM/DMARC failures and enabling spoofing.
**Learning:** This is a common pattern that frequently fails in production. Email APIs (like Resend) strictly enforce domain verification for the `From` address. Using user input directly results in the email being rejected or marked as spam.
**Prevention:** Always use an application-owned, verified email address in the `From` header. Add the user-provided email to the `Reply-To` header to allow easy responses while maintaining email deliverability and security.
## 2026-05-28 - Moderation Bypass Fix
**Vulnerability:** The image upload endpoint skipped all moderation checks and allowed any file if the `ModerationURL` was unconfigured.
**Learning:** Security pipelines should fail-closed. Treating missing security services as a graceful degradation defeats the purpose of the pipeline.
**Prevention:** Explicitly reject requests (fail-closed) when mandatory security dependencies (like the ModerationService) are null or fail, and write unit tests asserting this behavior.
