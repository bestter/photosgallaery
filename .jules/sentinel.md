## $(date +%Y-%m-%d) - Prevent Default Database Credentials Deployment
 **Vulnerability:** Placeholder database credentials ("YOUR_DB_SERVER", etc.) left in `appsettings.json` can cause broken connections if deployed, or potentially unauthorized access if a default service is exposed.
 **Learning:** Emptying connection strings in configuration files is insufficient if it causes obscure errors at startup (e.g., when `ServerVersion.AutoDetect` fails).
 **Prevention:** Explicitly validate connection strings in `Program.cs` during startup. If null, empty, or containing placeholder text, fail fast with a clear, localized `InvalidOperationException` to guide developers to correctly configure the environment.
