# download_model.py
from transformers import pipeline

print("Téléchargement du modèle en cours...")
# En chargeant le pipeline une fois, les fichiers sont mis en cache localement
pipeline("image-classification", model="Falconsai/nsfw_image_detection")
print("Modèle téléchargé et mis en cache avec succès !")