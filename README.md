# PixelLyra 🌌

PixelLyra est une application web moderne et performante de gestion, partage et modération de photos. Elle combine une interface utilisateur immersive au style cyberpunk minimaliste avec une API résiliente et sécurisée.

Le projet met en œuvre des fonctionnalités avancées comme la génération asynchrone de miniatures, l'extraction de métadonnées EXIF/GPS, la modération automatique d'images assistée par IA, et un système robuste d'authentification basé sur des cookies sécurisés.

---

## 📖 Liens et Documentation Clés

* **[Analyse Technique et Architecture Complète (project_analysis.md)](file:///C:/Users/marti/.gemini/antigravity-cli/brain/694679d4-6da2-493f-8746-fda0929a7558/project_analysis.md)** : Rapport complet détaillant la base de données, la logique de cache, les workers d'arrière-plan (files d'attente asynchrones), le microservice de modération, ainsi que les stratégies de performance.
* **[Conventions de Développement (AGENTS.md)](file:///C:/Users/marti/source/repos/PhotoApp/AGENTS.md)** : Règles strictes concernant les modifications de code, la localisation i18n, et l'écriture de commits atomiques.
* **[Design System « Cyanide Glass » (DESIGN.md)](file:///C:/Users/marti/source/repos/PhotoApp/PhotoFrontend/DESIGN.md)** : Spécifications visuelles détaillées du thème (couleurs, typographie, effets lumineux et animations).

---

## 🛠️ Stack Technologique

### 1. Frontend (`PhotoFrontend`)
* **Framework** : React 19 + Vite.
* **Styling** : Tailwind CSS v4 (Cyberpunk Glassmorphism).
* **Localisation** : `react-i18next` (support complet du Français et de l'Anglais sans textes codés en dur).
* **Requêtes et État** : Axios (avec `withCredentials` pour les cookies sécurisés).
* **Tests** : Vitest, React Testing Library.

### 2. Backend API (`PhotoAppApi`)
* **Framework** : C# / ASP.NET Core (.NET 10.0).
* **Base de données** : MariaDB / MySQL avec Entity Framework Core (Pomelo).
* **Stockage d'objets** : AWS SDK pour S3 (connecté sur Cloudflare R2).
* **Traitement d'images** : SixLabors.ImageSharp.
* **Workers de Fond** : Traitement asynchrone par `BoundedChannel` pour l'incrémentation des vues et le calcul de hachages.
* **Logging** : Log4Net.
* **Tests** : xUnit.

### 3. Microservice de Modération (`moderation-service`)
* **Framework** : FastAPI + Python.
* **Modèle IA** : Détection d'images inappropriées via le modèle `Falconsai/nsfw_image_detection` (Transformers / PyTorch).

### 4. Infrastructure & Déploiement
* **Conteneurisation** : Docker (build multi-étapes combinant le bundle statique React dans le serveur C#).
* **Hébergement** : Fly.io.

---

## 🚦 Démarrage Rapide

### Prérequis
* .NET 10.0 SDK
* Node.js (v20+)
* Base de données MySQL / MariaDB locale ou distante
* Python 3.10+ (uniquement si vous lancez le service de modération en local)

### Étape 1 : Configuration et Lancement de l'API Backend
1. Naviguez dans le répertoire de l'API : `cd PhotoAppApi`
2. Configurez vos secrets dans le fichier `appsettings.json` ou via les variables d'environnement (chaîne de connexion MySQL, clé secrète JWT d'au moins 64 caractères, identifiants ObjectStorage S3/R2).
3. Appliquez les migrations de base de données si nécessaire.
4. Lancez le serveur : `dotnet run`

### Étape 2 : Configuration et Lancement du Frontend React
1. Naviguez dans le répertoire frontend : `cd PhotoFrontend`
2. Installez les paquets : `npm install`
3. Démarrez le serveur de développement Vite : `npm run dev`
4. L'application est accessible par défaut sur [http://localhost:5173](http://localhost:5173).

---

## 🧪 Tests et Validation du Code

Le projet impose le passage réussi de toutes les suites de tests avant validation :
* **Backend C#** : Exécutez `dotnet test` dans le répertoire [PhotoAppApi.Tests](file:///C:/Users/marti/source/repos/PhotoApp/PhotoAppApi.Tests) ou à la racine.
* **Frontend React** : Exécutez `npm run test` (ou `npx vitest run`) dans [PhotoFrontend](file:///C:/Users/marti/source/repos/PhotoApp/PhotoFrontend).
* **Linter Frontend** : Exécutez `npm run lint` pour s'assurer qu'aucun warning ou erreur de syntaxe n'est présent.
