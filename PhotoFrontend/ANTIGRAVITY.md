# Configuration Antigravity - Frontend (React / Vite)

## 1. Contexte de l'environnement local
* **Rôle :** Interface utilisateur de l'application de galerie photo.
* **Stack :** React, Vite, Node.js.
* **Gestionnaire de paquets :** npm.

## 2. Comportement dans l'éditeur et le terminal
* **Serveur de développement :** Utilise `npm run dev` dans le terminal pour démarrer le serveur local Vite.
* **Aperçu interactif :** Dès que le serveur Vite est prêt, ouvre automatiquement le navigateur intégré de l'IDE sur `http://localhost:5173`. Cela me permet de valider visuellement et instantanément les changements d'interface (comme le rendu visuel du menu déroulant des langues).
* **Contrôle qualité :** Si tu modifies massivement des composants, lance `npm run lint` (ou la commande de build Vite) en arrière-plan. Si des erreurs de syntaxe simples sont détectées, corrige-les de manière autonome avant de me présenter le résultat.

## 3. Règles d'édition du code React
* **Architecture des composants :** Génère exclusivement des composants fonctionnels modernes basés sur les Hooks.
* **Gestion des textes (i18n) :** Garde un œil strict sur l'internationalisation. Ne code jamais de chaînes de caractères en dur dans le JSX. Si je te demande d'ajouter un nouveau bouton ou un titre, utilise d'emblée le hook `useTranslation()` de `react-i18next` et rappelle-moi de mettre à jour les fichiers `fr.json` et `en.json` correspondants dans le dossier public.
* **Mises à jour optimistes :** Pour toute interaction liée à la base de données (comme cliquer sur le bouton pour aimer une photo), mets à jour l'état local de l'interface immédiatement pour garantir une sensation de fluidité, sans attendre le retour de l'API.

## 4. Maintenance de l'infrastructure
* **Environnement et Déploiement :** Le frontend sera déployé via un conteneur Docker sur Fly.io. Si tu as besoin d'ajouter une nouvelle variable d'environnement (dans le `.env` local pour Vite), vérifie toujours que le `Dockerfile` du frontend expose ou utilise correctement cette variable lors de l'étape `npm run build`.