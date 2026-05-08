# Directives pour Agents IA (AGENTS.md)

## 1. Stack Technologique Principal
* **Frontend :** React / Vite
* **Backend :** C# / ASP.NET Core
* **Base de données :** MariaDB
* **Conteneurisation & Déploiement :** Docker / Fly.io
* **Internationalisation (i18n) :** react-i18next (Langues supportées : FR, EN)

## 2. Conventions de Développement

### Frontend (React & Vite)
* Utiliser exclusivement des composants fonctionnels et des Hooks.
* **Architecture et Design :** Toute modification de l'interface ou ajout de composants doit être strictement conforme aux règles visuelles définies dans `DESIGN.md` et respecter les configurations d'environnement de `ANTIGRAVITY.md`. Ces fichiers sont la source de vérité. En cas de contradiction, DESIGN.md et ANTIGRAVITY.md priment sur tout le reste. Si un cas d'usage n'est pas couvert, l'agent doit demander des instructions au lieu d'improviser.
* Aucune chaîne de caractères codée en dur dans l'interface. Tout texte doit être enveloppé par le hook `useTranslation()` de `react-i18next`.
* Les mises à jour de l'interface liées aux interactions (ex: compteurs de likes, ajout de tags) doivent être optimistes pour garantir la fluidité.

### Backend (C# .NET Core)
* Garder les contrôleurs légers. La logique métier doit être extraite dans des services indépendants et injectables, notamment pour :
  * Le téléversement sécurisé des images (secure uploads).
  * Le traitement et la gestion du système de balises/tags d'images.
  * La génération de miniatures et l'extraction de métadonnées EXIF/GPS.
* Maintenir une stricte intégrité référentielle dans MariaDB. Gérer les accès concurrents (race conditions) et assurer la suppression en cascade propre des entités dépendantes (likes, rapports) lors de la suppression d'une image.
* Utiliser des standards cryptographiques robustes (ex: SHA512) pour les opérations de hachage.
* Respecter les configurations d'environnement de `ANTIGRAVITY.md`. Ce fichier est la source de vérité pour le backend. Si un cas d'usage n'est pas couvert, l'agent doit demander des instructions au lieu d'improviser.

## 3. Directives de Déploiement et d'Infrastructure
* Les services sont déployés sur Fly.io via des conteneurs.
* Toute modification impliquant de nouvelles dépendances système, des changements de ports, ou l'ajout de variables d'environnement (spécialement celles préfixées par `VITE_` pour le frontend) doit être impérativement reflétée dans les `Dockerfile` correspondants.

## 4. Règles de Conduite de l'Agent
* **Formatage du Code :** Avant de générer, de modifier du code ou de soumettre un *diff*, analyser et respecter impérativement les règles d'indentation, de retours à la ligne et de style définies dans le fichier `.editorconfig` à la racine du projet.
* **Minimalisme :** Prioriser un code fonctionnel, durable et facilement maintenable. Éviter toute sur-ingénierie et ne pas introduire de bibliothèques tierces non nécessaires.
Ne jamais ajouter de nouvelle dépendance npm / NuGet sans validation explicite (même pour des utilitaires « petits » )
* **Validation :** Toute modification de la logique d'affaire ou de l'accès aux données doit être accompagnée des tests unitaires ou d'intégration appropriés avant la soumission d'un Pull Request.
* **Langue :** Rédiger la documentation technique, les commentaires de code et les messages de commit en anglais, sauf les fichiers AGENTS.md et ANTIGRAVITY.md qui doivent être rédigés en français.

