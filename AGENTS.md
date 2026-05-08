# Directives pour Agents IA (AGENTS.md)

## Golden Rules – Règles Absolues (Ne jamais transgresser)

1. **Respecte DESIGN.md et ANTIGRAVITY.md à la lettre**
   Toute modification d’interface, de composant ou de style doit être **strictement conforme** aux règles définies dans `DESIGN.md`. Les configurations d'environnement de `ANTIGRAVITY.md` priment sur le reste. En cas de contradiction, ces fichiers sont la source de vérité. En cas de doute ou de cas non couvert → **demande confirmation** avant de coder.

2. **Zéro chaîne de caractères hardcodée**
   Aucun texte visible par l’utilisateur ne doit être écrit directement dans le code. Tout passe obligatoirement par le hook `useTranslation()` de `react-i18next`.

3. **Minimalisme extrême**
   Priorise toujours un code fonctionnel, durable et facilement maintenable. Évite toute sur-ingénierie. **Aucune nouvelle dépendance** (npm ou NuGet) ne doit être ajoutée sans validation explicite (même pour des utilitaires « petits »).

4. **Demande avant d’improviser**
   Si une fonctionnalité, un pattern ou une décision d’architecture n’est pas clairement documenté dans `AGENTS.md`, `DESIGN.md` ou `ANTIGRAVITY.md` → **pose la question** au lieu de deviner.

5. **Respecte .editorconfig + langue dans le code**
   Avant de générer ou de modifier du code, analyse et respecte **impérativement** les règles du fichier `.editorconfig`. Tous les commentaires de code, messages de commit et documentation technique doivent être rédigés **en anglais**, à l'exception des fichiers `AGENTS.md` et `ANTIGRAVITY.md` qui doivent rester en français.

---

## 1. Stack Technologique Principal
* **Frontend :** React / Vite
* **Backend :** C# / ASP.NET Core
* **Base de données :** MariaDB
* **Conteneurisation & Déploiement :** Docker / Fly.io
* **Internationalisation (i18n) :** react-i18next (Langues supportées : FR, EN)

## 2. Conventions de Développement

### Frontend (React & Vite)
* Utiliser exclusivement des composants fonctionnels et des Hooks.
* Les mises à jour de l'interface liées aux interactions (ex: compteurs de likes, ajout de tags) doivent être optimistes pour garantir la fluidité.

### Backend (C# .NET Core)
* Garder les contrôleurs légers. La logique métier doit être extraite dans des services indépendants et injectables, notamment pour :
  * Le téléversement sécurisé des images (secure uploads).
  * Le traitement et la gestion du système de balises/tags d'images.
  * La génération de miniatures et l'extraction de métadonnées EXIF/GPS.
* Maintenir une stricte intégrité référentielle dans MariaDB. Gérer les accès concurrents (race conditions) et assurer la suppression en cascade propre des entités dépendantes (likes, rapports) lors de la suppression d'une image.
* Utiliser des standards cryptographiques robustes (ex: SHA512) pour les opérations de hachage.

## 3. Directives de Déploiement et d'Infrastructure
* Les services sont déployés sur Fly.io via des conteneurs.
* Toute modification impliquant de nouvelles dépendances système, des changements de ports, ou l'ajout de variables d'environnement (spécialement celles préfixées par `VITE_` pour le frontend) doit être impérativement reflétée dans les `Dockerfile` correspondants.

## 4. Tests et Validation
* Toute modification de la logique d'affaire ou de l'accès aux données doit être accompagnée des tests unitaires ou d'intégration appropriés avant la soumission d'un Pull Request.
