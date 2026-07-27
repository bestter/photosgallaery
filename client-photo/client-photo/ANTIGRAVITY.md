# Directives IA : Client Photo Legacy / Module Secondaire

## 1. Règles Globales
* Ne jamais indexer, lire ou analyser les dossiers `node_modules`, `bin` ou `obj`.
* Respecter l'organisation du code et éviter toute régression.

## 2. Règles Backend (C# / .NET / MariaDB)
* Traitement d'images via `SixLabors.ImageSharp`.
* Intégration légère des contrôleurs et délégation de la logique métier aux services d'application.

## 3. Règles Frontend (React)
* Composants fonctionnels purs et hooks.
* Gestion rigoureuse des états de chargement, d'erreur et des retours API.