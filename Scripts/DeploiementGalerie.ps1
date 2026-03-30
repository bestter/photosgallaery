# ==========================================
# Arrêter le script immédiatement sur les erreurs PowerShell
# ==========================================
$ErrorActionPreference = "Stop"

# ==========================================
# Configuration
# ==========================================

# ==========================================
# Configuration sécurisée (Lecture du fichier JSON)
# ==========================================
$ConfigFile = Join-Path $PSScriptRoot "deploy-settings.json"

if (-Not (Test-Path $ConfigFile)) {
    throw "❌ ERREUR : Le fichier de configuration '$ConfigFile' est introuvable. Veuillez le créer pour sécuriser vos mots de passe."
}

$Config = Get-Content $ConfigFile | ConvertFrom-Json

# Variables chargées depuis le fichier JSON
$ServerUserHost = $Config.ServerUserHost
$DbConnectionString = $Config.DbConnectionString

$BackendLocalPath = "C:\Users\marti\source\repos\PhotoApp\PhotoAppApi\"
$FrontendLocalPath = "C:\Users\marti\source\repos\PhotoApp\PhotoFrontend\"

$BackendRemotePath = "/var/www/magalerie/api"  # Dossier de destination backend
$FrontendRemotePath = "/var/www/magalerie/frontend" # Dossier de destination frontend
$ServiceName = "magalerie-api.service"             # Nom du service systemd


$PublishDir = Join-Path $PSScriptRoot "publish_temp"

Write-Host "Démarrage du déploiement de la galerie photo..." -ForegroundColor Green

try {
    if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
    New-Item -ItemType Directory -Path $PublishDir | Out-Null

    # ==========================================
    # 1. Base de données (MariaDB)
    # ==========================================
    Write-Host "`n[1/5] Préparation de la migration MariaDB..." -ForegroundColor Cyan
    Push-Location $BackendLocalPath


    # Remarque : on utilise directement $PublishDir car c'est maintenant un chemin absolu !
    dotnet ef migrations bundle --self-contained -r linux-x64 --force -o "$PublishDir\efbundle"
    if ($LASTEXITCODE -ne 0) { throw "Échec de la création du bundle de migration de base de données." }
    Pop-Location

    # ==========================================
    # 2. Compilation Frontend (React)
    # ==========================================
    Write-Host "`n[2/5] Compilation du Frontend (React)..." -ForegroundColor Cyan
    Push-Location $FrontendLocalPath
    npm install
    if ($LASTEXITCODE -ne 0) { throw "Échec du 'npm install' pour le frontend." }

    node increment-build.js
    
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "Échec de la compilation 'npm run build' pour le frontend." }
    Pop-Location

    # ==========================================
    # 3. Compilation Backend (.NET)
    # ==========================================
    Write-Host "`n[3/5] Compilation du Backend C#..." -ForegroundColor Cyan
    dotnet publish $BackendLocalPath -c Release -r linux-x64 --self-contained false -o "$PublishDir\backend"
    if ($LASTEXITCODE -ne 0) { throw "Échec de la compilation 'dotnet publish' du backend." }

    # NOUVELLE LIGNE : On supprime les photos de test locales du paquet de déploiement
    Write-Host "  -> Nettoyage des données de test locales..."
    $LocalImagesDir = "$PublishDir\backend\wwwroot\images"
    if (Test-Path $LocalImagesDir) { Remove-Item -Recurse -Force $LocalImagesDir }

    # ==========================================
    # 4. Transfert vers le serveur (SCP)
    # ==========================================
    Write-Host "`n[4/5] Transfert des fichiers vers le serveur Linux..." -ForegroundColor Cyan
    
    ssh $ServerUserHost "mkdir -p $FrontendRemotePath $BackendRemotePath"
    if ($LASTEXITCODE -ne 0) { throw "Impossible de créer les dossiers de destination sur le serveur." }
    
    Write-Host "  -> Envoi du Frontend..."
    scp -r "$FrontendLocalPath\dist\*" "${ServerUserHost}:${FrontendRemotePath}"
    if ($LASTEXITCODE -ne 0) { throw "Échec du transfert SCP pour le Frontend (Vérifie les permissions sur le serveur)." }
    
    Write-Host "  -> Envoi du Backend..."
    scp -r "$PublishDir\backend\*" "${ServerUserHost}:${BackendRemotePath}"
    if ($LASTEXITCODE -ne 0) { throw "Échec du transfert SCP pour le Backend (Vérifie les permissions sur le serveur)." }

    # NOUVELLE LIGNE : On donne le droit de lecture à Apache, et on ignore les erreurs sur le dossier images
    Write-Host "  -> Ajustement des permissions pour Apache..."
    ssh $ServerUserHost "chmod -R 755 $FrontendRemotePath $BackendRemotePath 2>/dev/null || true"
    if ($LASTEXITCODE -ne 0) { throw "Échec du changement de permissions (chmod)." }
    
    Write-Host "  -> Envoi et exécution de la migration DB..."
    scp "$PublishDir\efbundle" "${ServerUserHost}:/tmp/efbundle"
    if ($LASTEXITCODE -ne 0) { throw "Échec du transfert du bundle de base de données." }
    
    ssh $ServerUserHost "chmod +x /tmp/efbundle && /tmp/efbundle --connection '$DbConnectionString'"
    if ($LASTEXITCODE -ne 0) { throw "La migration de la base de données a échoué sur le serveur." }

    # ==========================================
    # 5. Redémarrage
    # ==========================================
    Write-Host "`n[5/5] Redémarrage du service d'API..." -ForegroundColor Cyan
    ssh $ServerUserHost "sudo systemctl restart $ServiceName"
    if ($LASTEXITCODE -ne 0) { throw "Échec du redémarrage du service $ServiceName via systemctl." }

    Write-Host "`n✅ Déploiement terminé avec succès !" -ForegroundColor Green

}
catch {
    Write-Host "`n❌ DÉPLOIEMENT ANNULÉ : Une erreur est survenue." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
    if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
    Set-Location $PSScriptRoot
}