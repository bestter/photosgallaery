# Directives pour Agents IA (AGENTS.md)

## Golden Rules – Règles Absolues (Ne jamais transgresser)

1. **Respecte DESIGN.md et ANTIGRAVITY.md à la lettre**
   Toute modification d’interface, de composant ou de style doit être **strictement conforme** aux règles définies dans `DESIGN.md`. Les configurations d'environnement des fichiers `ANTIGRAVITY.md` priment sur le reste. En cas de contradiction, ces fichiers sont la source de vérité. En cas de doute ou de cas non couvert → **demande confirmation** avant de coder.

2. **Ne modifie jamais les fichiers AGENTS.md et ANTIGRAVITY.md sans autorisation explicite**
   Ces fichiers sont la source de vérité pour l'agent IA et doivent être respectés à la lettre.

3. **Zéro chaîne de caractères hardcodée**
   Aucun texte visible par l’utilisateur ne doit être écrit directement dans le code frontend. Tout passe obligatoirement par le hook `useTranslation()` de `react-i18next`.

4. **Minimalisme extrême**
   Priorise toujours un code fonctionnel, durable et facilement maintenable. Évite toute sur-ingénierie. **Aucune nouvelle dépendance** (npm ou NuGet) ne doit être ajoutée sans validation explicite (même pour des utilitaires « petits »).

5. **Demande avant d’improviser**
   Si une fonctionnalité, un pattern ou une décision d’architecture n’est pas clairement documenté dans `AGENTS.md`, `DESIGN.md` ou `ANTIGRAVITY.md` → **pose la question** au lieu de deviner.

6. **Respecte .editorconfig + langue dans le code**
   Avant de générer ou de modifier du code, analyse et respecte **impérativement** les règles du fichier `.editorconfig`. Tous les commentaires de code, messages de commit et documentation technique doivent être rédigés **en anglais**, à l'exception des fichiers `AGENTS.md` et `ANTIGRAVITY.md` qui doivent rester en français.

---

## 1. Stack Technologique Principal

* **Frontend :** React 19 / Vite + Tailwind CSS v4 ("Cyanide Glass")
* **Backend :** C# / ASP.NET Core (.NET 10.0)
* **Base de données :** MariaDB / MySQL (Entity Framework Core / Pomelo avec `EnableRetryOnFailure`)
* **Stockage Objets :** AWS SDK S3 (connecté sur Cloudflare R2) + ASP.NET DataProtection Key Repository R2
* **Microservice IA :** FastAPI + Python (Hugging Face Transformers / PyTorch `Falconsai/nsfw_image_detection`)
* **Conteneurisation & Déploiement :** Docker (build multi-étapes) / Fly.io (`pixellyra`, `ton-moderation-service`)
* **Internationalisation (i18n) :** react-i18next (Langues supportées : FR, EN)

---

## 2. Conventions de Développement

### Frontend (React & Vite)

* Utiliser exclusivement des composants fonctionnels modernes et des Hooks (`useState`, `useEffect`, `useCallback`, `useMemo`).
* Les mises à jour de l'interface liées aux interactions (ex: compteurs de likes, ajout/suppression de tags) doivent être optimistes pour garantir la fluidité.
* Conserver les appels API centralisés dans `src/api.js` avec gestion automatique des cookies de session (`withCredentials`) et transmission des jetons anti-CSRF (`X-CSRF-TOKEN`).
* Valider systématiquement l'absence de texte en dur et exécuter `npm run lint` ou `npx react-doctor@latest --scope changed` après toute modification UI majeure.

### Backend (C# .NET Core)

* Garder les contrôleurs légers (`PhotosController`, `AuthController`, `AdminController`, `GroupsController`, `TagsController`, etc.). La logique métier doit être extraite dans des services indépendants et injectables, notamment pour :
  * Le téléversement et redimensionnement sécurisé d'images (`SixLabors.ImageSharp`).
  * L'extraction des métadonnées EXIF/GPS et le calcul de hachages d'images.
  * Le traitement asynchrone des vues via file d'attente bornée (`BoundedChannel<PhotoViewEvent>`) et `PhotoViewProcessingWorker`.
  * La modération d'images via HTTP client avec l'en-tête `X-API-Key` sur le service FastAPI.
* Maintenir une stricte intégrité référentielle dans MariaDB. Gérer les accès concurrents (race conditions) et assurer la suppression en cascade propre des entités dépendantes (likes, rapports, vues) lors de la suppression d'une image.
* Sécurité et cache :
  * Invalidation dynamique des tokens JWT en cas de changement de rôle ou de bannissement (`IMemoryCache` avec clé `UserValidV2_{userId}`).
  * Rate limiting configuré par politiques ASP.NET Core pour la protection contre le déni de service (brute force login, uploads, rapports, etc.).
  * Logging unifié via log4net (`AddLog4net()`).

---

## 3. Directives de Déploiement et d'Infrastructure

* Les services sont déployés sur Fly.io via des conteneurs Docker.
* Toute modification impliquant de nouvelles dépendances système, des changements de ports, ou l'ajout de variables d'environnement (spécialement celles préfixées par `VITE_` pour le frontend) doit être impérativement reflétée dans les `Dockerfile` correspondants et dans `fly.toml`.
* Les clés secrètes d'environnement (`MODERATION_API_KEY`, `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `ObjectStorage__*`) doivent être gérées via Fly secrets ou variables sécurisées.

---

## 4. Tests et Validation

* **Audit de l'existant :** Avant toute modification, vérifier systématiquement si des tests existent déjà pour le code ciblé (**xUnit** pour le backend C# dans `PhotoAppApi.Tests`, **Vitest** pour le frontend React dans `PhotoFrontend`). Si la logique métier change, ces tests doivent impérativement être mis à jour pour éviter toute régression.
* **Exécution systématique :**
  - **Backend** : `dotnet test` (129 tests d'intégration et unitaires).
  - **Frontend** : `npm run test -- --run` (35 tests unitaires).
* **Validation stricte :** S'assurer que 100% de la suite de tests passe avec succès avant d'effectuer un commit ou de finaliser une PR.

---

## 5. Conventions Git et Historique

* **Gestion des branches (Branching) :** Ne pousse **jamais** de code directement sur les branches principales (`main` ou `master`). Crée toujours une branche de travail distincte, courte et descriptive (ex: `feature/nom-de-la-fonctionnalite`, `bugfix/nom-du-bug`, `refactor/nom-du-composant`).
* **Pull Requests (PR) :** Tout changement doit obligatoirement passer par la création d'une Pull Request. Aucune fusion (merge) ne doit être effectuée sans une revue préalable.
* **Commits atomiques :** Chaque commit doit représenter une unité de travail cohérente et complète. Évite les commits qui mélangent plusieurs changements non liés.
* **Format des messages :** Utilise la spécification *Conventional Commits* (`feat:`, `fix:`, `refactor:`, `chore:`, etc.) pour structurer les titres de tes commits.
* **Signature de l'IA :** Signe toujours tes commits avec ton nom et ton nom de modèle exact à la fin de la description (ex: `Generated-by: Gemini 3.6 Flash`).
* **Langue :** Conformément à la Règle Dorée #6, l'intégralité des messages de commit (titre et description) doit être rédigée **strictement en anglais**.

