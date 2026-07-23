# PixelLyra Frontend 🌌

PixelLyra Frontend est l'interface utilisateur de la plateforme de gestion et partage de photos. Elle est construite avec React 19 et Vite, et intègre un système de design personnalisé "Cyanide Glass" au style Cyberpunk Glassmorphic fondé sur Tailwind CSS v4.

---

## 🚀 Fonctionnalités Principales

- **Galerie & Modale Photo** : Affichage immersif des photos avec navigation fluide, réactions (likes optimistes), extraction EXIF/GPS et intégration de la carte interactive Leaflet.
- **Téléversement & Tags** : Envoi d'images par glisser-déposer (dropzone), prévisualisation instantanée, catégorisation par groupes et système de mots-clés.
- **Gestion des Groupes & Invitations** : Création de groupes, demande d'accès, modales d'invitation et gestion administrative des membres.
- **Espace Administrateur & Modération** : Interface d'administration pour la validation des demandes de groupe et le traitement des rapports d'images signalées.
- **Internationalisation (i18n)** : Support bilingue dynamique (Français / Anglais) via `react-i18next`.
- **Authentification Sécurisée** : Session par cookies HttpOnly JWT, protection anti-CSRF (`X-CSRF-TOKEN`) et gestion automatique du rafraîchissement d'état.

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
Lance les tests unitaires et de composants avec Vitest et React Testing Library.

### `npm run lint`
Vérifie la qualité du code avec ESLint.

### `npx react-doctor@latest --scope changed`
Audite les composants modifiés pour détecter les régressions de performance, d'accessibilité et de hooks.

