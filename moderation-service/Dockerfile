# Utiliser une image Python minimaliste
FROM python:3.11-slim

# Définir le répertoire de travail
WORKDIR /app

# Définir les variables d'environnement pour forcer HuggingFace à utiliser le dossier local
ENV TRANSFORMERS_CACHE=/app/.cache
ENV HF_HOME=/app/.cache
# Empêcher Python d'écrire des fichiers .pyc inutiles
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

# Installer les dépendances système minimales (si nécessaire pour compiler certains paquets)
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Copier et installer les dépendances Python
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copier le script de téléchargement et récupérer le modèle
COPY download_model.py .
RUN python download_model.py

# Copier le reste du code de l'API (votre fichier main.py)
COPY . .

# Exposer le port par défaut de Fly.io
EXPOSE 8080

# Démarrer l'application avec un seul "worker" pour limiter la RAM
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8080", "--workers", "1"]