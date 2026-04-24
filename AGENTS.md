# Directives pour Agents IA (AGENTS.md)

## 1. Stack Technologique Principal
* **Frontend :** React
* **Backend :** C# / ASP.NET Core
* **Base de données :** MariaDB
* **Conteneurisation & Déploiement :** Docker / Fly.io
* **Internationalisation (i18n) :** react-i18next (Langues supportées : FR, EN)

## 2. Conventions de Développement

### Frontend (React)
* Utiliser exclusivement des composants fonctionnels et des Hooks.
* Aucune chaîne de caractères codée en dur dans l'interface. Tout texte doit être enveloppé par le hook `useTranslation()` de `react-i18next`.
* Les mises à jour de l'interface liées aux interactions (ex: compteurs de likes) doivent être optimistes pour garantir la fluidité.

### Backend (C# .NET Core)
* Garder les contrôleurs légers. La logique métier (génération de miniatures, extraction de métadonnées EXIF/GPS, traitement par lots) doit être extraite dans des services indépendants et injectables.
* Maintenir une stricte intégrité référentielle dans MariaDB. Gérer les accès concurrents (race conditions) et assurer la suppression en cascade propre des entités dépendantes (likes, rapports) lors de la suppression d'une image.
* Utiliser des standards cryptographiques robustes (ex: SHA512) pour les opérations de hachage.

## 3. Directives de Déploiement et d'Infrastructure
* Les services sont déployés sur Fly.io via des conteneurs. Toute modification impliquant de nouvelles dépendances système ou des changements de ports doit être impérativement reflétée dans les `Dockerfile` correspondants.

## 4. Règles de Conduite de l'Agent
* **Minimalisme :** Prioriser un code fonctionnel, durable et facilement maintenable. Éviter toute sur-ingénierie et ne pas introduire de bibliothèques tierces non nécessaires.
* **Validation :** Toute modification de la logique d'affaire ou de l'accès aux données doit être accompagnée des tests unitaires ou d'intégration appropriés avant la soumission d'un Pull Request.
* **Langue :** Rédiger la documentation technique, les commentaires de code et les messages de commit en anglais.