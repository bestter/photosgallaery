# PixelLyra 🌌

PixelLyra est une application web moderne et performante de gestion, partage et modération de photos. Elle combine une interface utilisateur immersive au style cyberpunk minimaliste ("Cyanide Glass") avec une API résiliente, sécurisée et à haute performance.

Le projet met en œuvre des fonctionnalités avancées :
- **Traitement asynchrone d'images** : génération de miniatures, conversion de formats et extraction de métadonnées EXIF/GPS via ImageSharp.
- **Modération automatique par IA** : analyse en temps réel du contenu des images via un microservice dédié FastAPI exploitant le modèle Hugging Face `Falconsai/nsfw_image_detection`.
- **Incrémentation haute performance** : enregistrement des vues par file d'attente asynchrone bornée (`BoundedChannel<PhotoViewEvent>`) et traitement en arrière-plan par `HostedService`.
- **Authentification & Sécurité renforcées** : authentification par jetons JWT (stockés dans des cookies sécurisés `SameSite/HttpOnly`), invalidation temps réel des sessions suspendues/modifiées via `IMemoryCache`, protection contre le CSRF (`X-CSRF-TOKEN`), limitation du débit (Rate Limiting par IP/Utilisateur) et en-têtes de sécurité stricts (CSP, HSTS, X-Frame-Options).
- **Stockage Objets R2/S3 & Clés Data Protection** : intégration AWS SDK S3 pour le stockage des fichiers et persistance des clés ASP.NET DataProtection sur Cloudflare R2.
- **Internationalisation (i18n)** : support bilingue complet (Français et Anglais) géré via `react-i18next` sans texte en dur dans l'interface.

---

## 📖 Liens et Documentation Clés

* **[Conventions de Développement pour Agent IA (AGENTS.md)](file:///C:/Users/marti/source/repos/PhotoApp/AGENTS.md)** : Directives strictes pour les agents de développement, règles de commit et processus de validation.
* **[Configuration Antigravity - API Backend (PhotoAppApi/ANTIGRAVITY.md)](file:///C:/Users/marti/source/repos/PhotoApp/PhotoAppApi/ANTIGRAVITY.md)** : Directives et comportements spécifiques au backend C# / ASP.NET Core.
* **[Configuration Antigravity - Frontend (PhotoFrontend/ANTIGRAVITY.md)](file:///C:/Users/marti/source/repos/PhotoApp/PhotoFrontend/ANTIGRAVITY.md)** : Directives et comportements spécifiques au frontend React / Vite.
* **[Design System « Cyanide Glass » (PhotoFrontend/DESIGN.md)](file:///C:/Users/marti/source/repos/PhotoApp/PhotoFrontend/DESIGN.md)** : Charte graphique cyberpunk glassmorphism, tokens de couleurs, typographies et composants UI.

---

## 🛠️ Stack Technologique

### 1. Frontend (`PhotoFrontend`)
* **Framework** : React 19 + Vite.
* **Styling** : Tailwind CSS v4 (Thème Cyberpunk Glassmorphism / Cyanide Glass).
* **Localisation** : `react-i18next` (support FR / EN).
* **Communication API** : Axios avec `withCredentials` (gestion automatique des cookies JWT et en-têtes CSRF).
* **Cartographie & Modales** : Leaflet / React-Leaflet pour la géolocalisation des photos.
* **Tests** : Vitest, React Testing Library.

### 2. Backend API (`PhotoAppApi`)
* **Framework** : C# / ASP.NET Core (.NET 10.0).
* **Base de données** : MariaDB / MySQL via Entity Framework Core avec Pomelo (`EnableRetryOnFailure`).
* **Stockage Objets** : AWS SDK S3 (connecté sur Cloudflare R2 / MinIO / AWS S3).
* **Traitement d'images** : SixLabors.ImageSharp.
* **Workers de Fond** : `PhotoViewProcessingWorker` (`BoundedChannel`) & `HashCalculationBackgroundService`.
* **Logging** : Log4net (`AddLog4net`).
* **Tests** : xUnit (129 tests unitaires et d'intégration).

### 3. Microservice de Modération (`moderation-service`)
* **Framework** : FastAPI + Python 3.10+.
* **Détection NSFW** : Pipeline Hugging Face Transformers avec le modèle `Falconsai/nsfw_image_detection`.
* **Sécurité** : Protection anti-Decompression Bomb (`Image.MAX_IMAGE_PIXELS`), limite de taille de 50 Mo et validation d'en-tête `X-API-Key`.

### 4. Infrastructure & Conteneurisation
* **Docker** : Build multi-étapes combinant la compilation du frontend React et la publication du binaire ASP.NET Core servi via `wwwroot`.
* **Déploiement** : Fly.io (`pixellyra` et `ton-moderation-service`).

---

## 🚦 Démarrage Rapide

### Prérequis
* **.NET 10.0 SDK**
* **Node.js (v20+)**
* **Base de données MariaDB ou MySQL**
* **Python 3.10+** (si exécution locale du service de modération)

### Étape 1 : Lancement du Backend API
1. Se positionner dans le dossier backend :
   ```bash
   cd PhotoAppApi
   ```
2. Configurer les variables dans `appsettings.json` ou via les variables d'environnement (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `FrontendUrl`, `ObjectStorage__*`).
3. Démarrer l'API :
   ```bash
   dotnet run
   ```

### Étape 2 : Lancement du Frontend React
1. Se positionner dans le dossier frontend :
   ```bash
   cd PhotoFrontend
   ```
2. Installer les dépendances :
   ```bash
   npm install
   ```
3. Lancer le serveur de développement Vite :
   ```bash
   npm run dev
   ```
4. L'application est disponible sur `http://localhost:5173`.

### Étape 3 : Lancement du Service de Modération (Optionnel)
1. Se positionner dans le dossier du microservice :
   ```bash
   cd moderation-service
   ```
2. Installer les dépendances Python :
   ```bash
   pip install -r requirements.txt
   ```
3. Lancer le serveur Uvicorn :
   ```bash
   uvicorn main:app --reload --port 8000
   ```

---

## 🧪 Tests et Validation du Code

Toutes les suites de tests doivent être validées sans échec avant toute soumission ou déploiement :

* **Tests Backend (C# xUnit)** :
  ```bash
  dotnet test
  ```
* **Tests Frontend (Vitest)** :
  ```bash
  cd PhotoFrontend
  npm run test -- --run
  ```
* **Linter Frontend (ESLint & React Doctor)** :
  ```bash
  cd PhotoFrontend
  npm run lint
  ```

