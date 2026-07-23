# Configuration Antigravity - Backend (C# .NET 10 / ASP.NET Core)

## 1. Contexte de l'environnement local

* **Rôle :** API Backend REST pour la plateforme de galerie photo PixelLyra.
* **Technologie :** ASP.NET Core (C# .NET 10.0).
* **Base de données :** MariaDB / MySQL via Entity Framework Core avec Pomelo (`EnableRetryOnFailure`).
* **Stockage Objets :** AWS SDK S3 connecté sur Cloudflare R2 + persistance des clés DataProtection (`CloudflareR2XmlRepository`).
* **Service d'IA Externe :** Client HTTP connecté au microservice FastAPI de modération d'images.
* **Logging & Observabilité :** log4net unifié (`AddLog4net()`).

---

## 2. Comportement dans l'éditeur et le terminal

* **Compilation automatique :** Après des modifications complexes (services, contrôleurs, helpers EXIF/GPS, hachage d'images), exécute `dotnet build` pour valider l'absence d'erreurs de syntaxe ou de types.
* **Tests unitaires et d'intégration :** Valide systématiquement les modifications avec `dotnet test` dans `PhotoAppApi.Tests` (129 tests unitaires et d'intégration).
* **Serveur de développement :** Utilise `dotnet run` (ou `dotnet watch`) pour démarrer l'API en local.

---

## 3. Gestion de la base de données (Entity Framework Core)

* **Surveillance des modèles :** Si tu modifies une entité du domaine (`Photo`, `User`, `Group`, `Tag`, `PhotoLike`, `PhotoView`, etc.), signale la modification du schéma.
* **Migrations :** Prépare la commande `dotnet ef migrations add <NomDescriptif>` dans le terminal, mais **attends toujours la confirmation** avant de l'exécuter ou de lancer `dotnet ef database update`.

---

## 4. Traitement d'arrière-plan et Haute Performance

* **Vues de photos (PhotoViewProcessingWorker) :** Les vues sont enregistrées dans un canal borné (`BoundedChannel<PhotoViewEvent>`, capacité 10 000) et dépilées de manière asynchrone par un `HostedService` sans bloquer les requêtes HTTP.
* **Calcul de hash (HashCalculationBackgroundService) :** Les hachages d'images sont calculés en arrière-plan.
* **Traitement d'images :** Redimensionnement et conversion de formats via `SixLabors.ImageSharp`.

---

## 5. Sécurité & Authentification

* **Gestion des Jetons JWT :** Jetons transmis via cookies sécurisés `jwt_token` ou paramètre d'URL `access_token` pour le chargement d'images.
* **Validation & Revocation temps réel :** Le middleware `JwtBearerEvents` vérifie `UserValidV2_{userId}` dans `IMemoryCache` (5 minutes de TTL) pour révoquer instantanément les accès en cas de bannissement (`UserRole.Forbidden`) ou de changement de rôle.
* **Rate Limiting & Anti-CSRF :** Politiques d'application strictes de débits par IP/Utilisateur et validation d'en-tête `X-CSRF-TOKEN`.

---

## 6. Maintenance Infrastructure (Fly.io & Docker)

* **Surveillance du conteneur :** En cas de changement de port, de dépendance système ou de configuration, mets à jour le `Dockerfile` multi-étapes et le fichier `fly.toml`.

