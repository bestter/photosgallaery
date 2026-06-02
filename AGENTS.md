# Directives pour Agents IA (AGENTS.md)

## Golden Rules – Règles Absolues (Ne jamais transgresser)

1. **Respecte DESIGN.md et ANTIGRAVITY.md à la lettre**
   Toute modification d’interface, de composant ou de style doit être **strictement conforme** aux règles définies dans `DESIGN.md`. Les configurations d'environnement de `ANTIGRAVITY.md` priment sur le reste. En cas de contradiction, ces fichiers sont la source de vérité. En cas de doute ou de cas non couvert → **demande confirmation** avant de coder.

2. **Ne modifie jamais les fichiers AGENTS.md et ANTIGRAVITY.md sans autorisation explicite**
   Ces fichiers sont la source de vérité pour l'agent IA et doivent être respectés à la lettre.

3. **Zéro chaîne de caractères hardcodée**
   Aucun texte visible par l’utilisateur ne doit être écrit directement dans le code. Tout passe obligatoirement par le hook `useTranslation()` de `react-i18next`.

4. **Minimalisme extrême**
   Priorise toujours un code fonctionnel, durable et facilement maintenable. Évite toute sur-ingénierie. **Aucune nouvelle dépendance** (npm ou NuGet) ne doit être ajoutée sans validation explicite (même pour des utilitaires « petits »).

5. **Demande avant d’improviser**
   Si une fonctionnalité, un pattern ou une décision d’architecture n’est pas clairement documenté dans `AGENTS.md`, `DESIGN.md` ou `ANTIGRAVITY.md` → **pose la question** au lieu de deviner.

6. **Respecte .editorconfig + langue dans le code**
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

* **Audit de l'existant :** Avant toute modification, vérifier systématiquement si des tests existent déjà pour le code ciblé (**xUnit** pour le backend C#, **Vitest** pour le frontend React). Si la logique d'affaire change, ces tests doivent impérativement être mis à jour pour éviter toute régression.
* **Couverture des nouveautés :** Toute nouvelle fonctionnalité ou modification touchant à l'accès aux données doit être accompagnée de nouveaux tests appropriés utilisant le framework correspondant.
* **Validation stricte :** S'assurer que l'ensemble de la suite de tests passe avec succès avant de soumettre un Pull Request ou de finaliser un *diff*.

## 5. Conventions Git et Historique

* **Gestion des branches (Branching) :** Ne pousse **jamais** de code directement sur les branches principales (`main` ou `master`). Crée toujours une branche de travail distincte, courte et descriptive (ex: `feature/nom-de-la-fonctionnalite`, `bugfix/nom-du-bug`, `refactor/nom-du-composant`).
* **Pull Requests (PR) :** Tout changement doit obligatoirement passer par la création d'une Pull Request. Aucune fusion (merge) ne doit être effectuée sans une revue préalable.
* **Commits atomiques :** Chaque commit doit représenter une unité de travail cohérente et complète. Évite les commits qui mélangent plusieurs changements non liés.
* **Format des messages :** Utilise la spécification *Conventional Commits* (`feat:`, `fix:`, `refactor:`, `chore:`, etc.) pour structurer les titres de tes commits.
* **Signature de l'IA :** Signe toujours tes commits avec ton nom et ton nom de modèle exact à la fin de la description (ex: `Generated-by: Hermes 2 Pro` ou `Generated-by: OpenClaw`). Cette traçabilité est essentielle pour auditer avec précision le code produit par les différents agents locaux ou distants.
* **Langue :** Conformément à la Règle Dorée #6, l'intégralité des messages de commit (titre et description) doit être rédigée **strictement en anglais**.
