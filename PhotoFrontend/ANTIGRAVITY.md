# Configuration Antigravity - Frontend (React 19 / Vite)

## 1. Contexte de l'environnement local

* **Rôle :** Interface utilisateur de la galerie photo PixelLyra.
* **Stack :** React 19, Vite, Tailwind CSS v4 ("Cyanide Glass").
* **Gestionnaire de paquets :** npm.
* **Localisation (i18n) :** `react-i18next` avec fichiers de traduction JSON (`public/locales/fr/translation.json`, `public/locales/en/translation.json`).

---

## 2. Comportement dans l'éditeur et le terminal

* **Serveur de développement :** Utilise `npm run dev` pour démarrer le serveur local Vite (`http://localhost:5173`).
* **Contrôle qualité & Linting :**
  - Exécute `npm run lint` pour la vérification ESLint.
  - Exécute `npx react-doctor@latest --scope changed` après des modifications substantielles de composants pour détecter les régressions d'accessibilité, de bundle ou de hooks.
* **Tests automatisés :** Exécute `npm run test -- --run` (Vitest) pour s'assurer que tous les tests de composants et d'aide (auth, api, cards, layout) passent avec succès.

---

## 3. Règles d'édition du code React

* **Architecture des composants :** Génère exclusivement des composants fonctionnels modernes basés sur les Hooks (`useState`, `useEffect`, `useCallback`, `useMemo`).
* **Système de Design (STRICT) :**
  - Applique impérativement les directives visuelles définies dans `PhotoFrontend/DESIGN.md` (Cyanide Glass Cyberpunk Glassmorphic design system).
  - Couleurs de fond (`#0e1515`, `#161d1d`), accents néon (`#00ced1`, `#47eaed`), typographie Inter, effet de flou arrière-plan (`backdrop-blur-md`), bordures subtiles (`cyan-400/10`) et ombres lumineuses.
  - Aucune classe CSS ou style UI ne doit déroger à `DESIGN.md`. En cas d'hésitation, demande confirmation.
* **Gestion des textes (i18n STRICT) :**
  - **Zéro texte hardcodé** dans le JSX. Tout libellé visible doit obligatoirement utiliser le hook `useTranslation()` de `react-i18next` (`t('namespace.key')`).
  - Mettre à jour les deux fichiers de traduction (`public/locales/fr/translation.json` et `public/locales/en/translation.json`) simultanément.
* **Mises à jour optimistes & Gestion API :**
  - Pour les réactions utilisateur (ex: likes, ajouts de tags, rapports), mets à jour l'état UI immédiatement de manière optimiste.
  - Tous les appels API transitent par `src/api.js` qui gère `withCredentials: true` et transmet le jeton anti-CSRF (`X-CSRF-TOKEN`).

---

## 4. Maintenance de l'infrastructure

* **Variables d'environnement :**
  - Toutes les variables d'environnement frontend doivent impérativement être préfixées par `VITE_` (ex: `VITE_API_URL`).
  - Vérifier la compatibilité avec le `Dockerfile` lors du `npm run build`.
* **Sécurité :** Ne jamais inclure de secrets ou jetons privés dans le code source React.

