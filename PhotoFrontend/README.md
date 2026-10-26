# PixelLyra Frontend 🌌

PixelLyra Frontend est l'interface utilisateur de la plateforme de gestion et partage de photos. Elle est construite avec React 19 et Vite, et intègre un système de design personnalisé "Cyanide Glass" au style Cyberpunk Glassmorphic fondé sur Tailwind CSS v4.

---

## 🚀 Fonctionnalités Principales

- **Galerie & Modale Photo** : Affichage immersif des photos avec navigation fluide, réactions (likes optimistes), extraction EXIF/GPS et intégration de la carte interactive Leaflet.
- **Téléversement & Tags** : Envoi d'images par glisser-déposer (dropzone) dans une modale fermable par le bouton Fermer, le clic sur le fond (backdrop) ou Échap, prévisualisation instantanée, catégorisation par groupes et système de mots-clés.
- **Gestion des Groupes & Invitations** : Création de groupes, demande d'accès, modales d'invitation et gestion administrative des membres.
- **Espace Administrateur & Modération** : Interface d'administration pour la validation des demandes de groupe et le traitement des rapports d'images signalées.
- **Internationalisation (i18n)** : Support bilingue dynamique (Français / Anglais) via `react-i18next`.
- **Authentification Sécurisée** : Session par cookies HttpOnly JWT, protection anti-CSRF (`X-CSRF-TOKEN`) avec rafraîchissement automatique et rejeu transparent des requêtes sur jeton expiré.

---

## 🎨 Design System : Cyanide Glass

Le design du frontend respecte rigoureusement la charte spécifiée dans `PhotoFrontend/DESIGN.md` :
- **Fond principal** : Dark Slate / Cyanide Tint (`#0e1515`, `#161d1d`).
- **Accents** : Electric Cyan (`#00ced1`, `#47eaed`) avec effets lumineux (neon drop-shadow).
- **Effets de profondeur** : Couches translucides et flou d'arrière-plan (`backdrop-blur-md`).
- **Typographie** : Famille Inter avec variations d'épaisseur et d'espacement (letter-spacing).

---

## 📦 Scripts Disponibles

Dans le répertoire `PhotoFrontend`, vous pouvez exécuter :

### `npm run dev`
Démarre le serveur de développement Vite sur `http://localhost:5173`.

### `npm run build`
Exécute la compilation de production et génère le paquet statique dans le dossier `dist`.

### `npm run test` (ou `npm run test -- --run`)
Lance la suite de 47 tests unitaires, de composants et de contrat d'API avec Vitest et React Testing Library.

### `npm run test:e2e` (ou `npx playwright test`)
Exécute la suite de 8 tests d'intégration End-to-End multi-navigateurs (Chromium et Firefox) avec Playwright.

### `npm run lint`
Vérifie la qualité du code avec ESLint.

### React Doctor

Run these commands from `PhotoFrontend`. Both use the latest published scanner without adding a project dependency.

| Command | Scope | Failure threshold |
| --- | --- | --- |
| `npm run doctor` or `npm run doctor:full` | Entire frontend, including existing findings | Errors |
| `npm run doctor:changed` | Only findings introduced since the merge base with `origin/main` | Warnings and errors |

Run `git fetch origin main` before the changed-scope check to refresh the target reference. To compare against a different target, use `npm run doctor:changed -- --base origin/target-branch`.

Append `-- --no-score` to either explicit command to skip the score API, share URL, and crash reporting. For example: `npm run doctor:changed -- --no-score`.

The active CI workflow is `/.github/workflows/react-doctor.yml`. On pull requests, it reports introduced findings with the default `changed` scope and remains advisory (`blocking: none`); pushes to `main` scan the full frontend. The Claude and Cursor hooks use `changed` scope with warnings treated as failures and skip scoring. These thresholds are intentional: a local regression check is stricter than the advisory CI report.

