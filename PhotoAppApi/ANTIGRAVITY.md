# Directives IA : Projet Galerie Photo

## 1. Règles Globales (Valide pour tout le projet)
* Ne jamais indexer, lire ou analyser les dossiers `node_modules`, `bin` ou `obj`.
* Limiter les réponses au code modifié, utiliser `// ... reste du code ...` pour économiser les tokens.

## 2. Règles BACKEND (C# / .NET / MariaDB)
* **Application :** Ces règles s'appliquent à tout ce qui se trouve dans le dossier `/BACKEND`.
* **Traitement d'images :** Pour toute manipulation (redimensionnement, conversion WebP), utilise systématiquement la bibliothèque `SixLabors.ImageSharp`.
* **Base de données :** Optimise les requêtes pour MariaDB. Garde les contrôleurs d'API légers et place la logique métier dans des services dédiés.
* **Journalisation (Logging) :** Toute fonction publique doit inclure une gestion des erreurs avec un log d'erreur parlant et utile via `log4net`. De plus, un log de niveau `DEBUG` doit être systématiquement généré lors de l'appel initial de ces fonctions pour faciliter le traçage.

## 3. Règles FRONTEND (React)
* **Application :** Ces règles s'appliquent à tout ce qui se trouve dans le dossier `/FRONTEND`.
* **Composants :** Privilégie les composants fonctionnels purs et les Hooks.
* **Communication API :** Assure-toi que toutes les requêtes vers le backend gèrent correctement les états de chargement (loading spinners) et d'erreur pour une bonne expérience utilisateur.