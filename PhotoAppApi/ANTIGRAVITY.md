# Configuration Antigravity - Backend (C# .NET Core)

## 1. Contexte de l'environnement local
* **Rôle :** API Backend pour l'application de galerie photo.
* **Technologie :** ASP.NET Core (C#).
* **Base de données locale :** MariaDB (interaction via Entity Framework Core).

## 2. Comportement dans l'éditeur et le terminal
* **Compilation automatique :** Après des modifications complexes (ex: refactoring du service d'extraction GPS/EXIF ou de la logique de hachage des images), lance silencieusement `dotnet build` en arrière-plan pour détecter les erreurs de syntaxe au plus vite.
* **Tests unitaires :** Si une modification touche à la logique d'accès concurrent (ex: le système de "likes" ou la suppression en cascade des rapports d'images), propose d'exécuter `dotnet test` dans le terminal pour valider l'intégrité de l'application.
* **Serveur de développement :** Lorsque demandé, utilise `dotnet run` (ou `dotnet watch`) pour démarrer l'API locale.

## 3. Gestion de la base de données (Entity Framework Core)
* **Surveillance des modèles :** Si tu modifies une entité du domaine (ex: ajout d'une colonne pour les traductions i18n ou les chemins des miniatures), alerte-moi que le schéma a changé.
* **Migrations :** Prépare la commande `dotnet ef migrations add <NomDescriptif>` dans le terminal, mais **attends toujours ma confirmation** avant de l'exécuter ou de lancer un `dotnet ef database update`.

## 4. Maintenance de l'infrastructure (Fly.io & Docker)
* **Surveillance du conteneur :** Si tu modifies la configuration des ports d'écoute de l'application (dans `appsettings.json` ou `launchSettings.json`) ou si tu ajoutes une dépendance nécessitant des librairies système spécifiques, mets à jour immédiatement le `Dockerfile` local pour garantir que le prochain déploiement sur Fly.io réussira.

## 5. Règles de Conduite de l'Agent
* **Formatage du Code :** Avant de générer, de modifier du code ou de soumettre un *diff*, analyser et respecter impérativement les règles d'indentation, de retours à la ligne et de style définies dans le fichier `.editorconfig` à la racine du projet.
* **Minimalisme :** Prioriser un code fonctionnel, durable et facilement maintenable. Éviter toute sur-ingénierie et ne pas introduire de bibliothèques tierces non nécessaires.
* **Validation :** Toute modification de la logique d'affaire ou de l'accès aux données doit être accompagnée des tests unitaires ou d'intégration appropriés avant la soumission d'un Pull Request.
* **Langue :** Rédiger la documentation technique, les commentaires de code et les messages de commit en anglais.
