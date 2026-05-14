# PixelLyra

PixelLyra is a full-stack, modern web application for managing and sharing photos. It features a robust backend API built with C# and ASP.NET Core, and a responsive, dynamic frontend built with React and Vite. The application is designed for secure image storage, efficient processing, and multi-language support.

## 🚀 Features

- **Photo Management**: Securely upload, view, and manage your photos.
- **Image Processing**: Automatic thumbnail generation and EXIF/GPS metadata extraction (using SixLabors.ImageSharp).
- **Secure Storage**: Integration with AWS S3 for secure, scalable object storage using pre-signed URLs.
- **Authentication & Security**: Robust JWT-based authentication and secure password hashing with BCrypt.
- **Internationalization (i18n)**: Full multi-language support (English and French) built with `react-i18next`.
- **Responsive UI**: Modern interface styled with Tailwind CSS v4, providing an optimistic UI for fluid interactions.
- **Email Notifications**: Integrated with Resend for transactional emails.

## 🛠️ Technology Stack

### Frontend (`PhotoFrontend`)
- **Framework**: React 19 + Vite
- **Styling**: Tailwind CSS v4
- **Localization**: `react-i18next`
- **State/Requests**: Axios, `jwt-decode`
- **Testing**: Vitest, React Testing Library

### Backend (`PhotoAppApi`)
- **Framework**: C# / ASP.NET Core (.NET 10)
- **Database**: MariaDB / MySQL with Entity Framework Core (Pomelo)
- **Object Storage**: AWS SDK for S3
- **Image Processing**: SixLabors.ImageSharp
- **Logging**: Log4Net
- **Testing**: xUnit

### Infrastructure & Deployment
- **Containerization**: Docker
- **Hosting**: Fly.io (`fly.toml` configuration included)

## 🏗️ Architecture & Best Practices

PixelLyra follows strict development guidelines (outlined in `AGENTS.md`):
- **Clean Backend Controllers**: Controllers remain lightweight, with business logic (such as secure uploads, tag management, and thumbnail generation) extracted into independent, injectable services.
- **Database Integrity**: Strict referential integrity in MariaDB, including proper cascading deletes for dependent entities (likes, reports).
- **Optimistic UI**: Frontend interactions (like counters and tagging) update optimistically for a seamless user experience.
- **Zero Hardcoded Strings**: All user-facing text is managed via `react-i18next` localization files.

## 🚦 Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js (v20+ recommended)
- MariaDB / MySQL Server
- AWS S3 Bucket (or compatible object storage)

### Backend Setup
1. Navigate to the API directory: `cd PhotoAppApi`
2. Configure your `appsettings.json` or User Secrets with your Database Connection String, JWT Settings, AWS S3 Credentials, and Resend API key.
3. Run Entity Framework migrations to set up the database schema.
4. Run the API: `dotnet run`

### Frontend Setup
1. Navigate to the frontend directory: `cd PhotoFrontend`
2. Install dependencies: `npm install`
3. Start the development server: `npm run dev`

## 🧪 Testing

The project maintains a strong emphasis on testing to prevent regressions:
- **Backend**: Run tests via `dotnet test` in the `PhotoAppApi.Tests` directory.
- **Frontend**: Run the Vitest suite via `npm run test` in the `PhotoFrontend` directory.

## 📄 License
This project is licensed under the terms specified in the `LICENSE` file.
