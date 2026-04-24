# --- ÉTAPE 1 : Build du frontend avec Node.js ---
FROM node:20-alpine AS build-front
WORKDIR /PhotoFrontend
COPY PhotoFrontend/package*.json ./
RUN npm install
COPY PhotoFrontend/ ./
# Génère les fichiers statiques dans /app/frontend/dist
RUN npm run build 

# --- ÉTAPE 2 : Build du backend avec C# ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-back
WORKDIR /PhotoAppApi
COPY PhotoAppApi/*.csproj ./
RUN dotnet restore
COPY PhotoAppApi/ ./
# MAGIE : On copie les fichiers React générés à l'étape 1 dans le dossier wwwroot du C#
COPY --from=build-front /PhotoFrontend/dist ./wwwroot
# On compile l'application C#
RUN dotnet publish PhotoAppApi.csproj -c Release -o out

# --- ÉTAPE 3 : Image finale (Très légère, sans Node ni SDK C#) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-back /PhotoAppApi/out .
EXPOSE 8080
# Remplace "MonApi.dll" par le nom exact de ton fichier de sortie C#
CMD ["dotnet", "PhotoAppApi.dll"]