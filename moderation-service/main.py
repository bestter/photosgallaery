from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import JSONResponse
from PIL import Image
import io
from transformers import pipeline
import torch

app = FastAPI(title="NSFW Moderation Service")

# Charger le modèle une seule fois au démarrage
print("Loading Falconsai model...")
classifier = pipeline(
    "image-classification", 
    model="Falconsai/nsfw_image_detection",
    device=0 if torch.cuda.is_available() else -1
)
print("Model loaded successfully!")

@app.post("/moderate")
async def moderate_image(file: UploadFile = File(...)):
    if file.content_type is None or not file.content_type.startswith("image/"):
        raise HTTPException(status_code=400, detail="File must be an image")

    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents)).convert("RGB")

        # Prédiction
        results = classifier(image)
        
        # Le modèle retourne généralement [{"label": "nsfw", "score": 0.XX}, {"label": "safe", "score": 0.XX}]
        nsfw_score = next((r["score"] for r in results if r["label"].lower() == "nsfw"), 0.0)
        safe_score = next((r["score"] for r in results if r["label"].lower() == "safe"), 0.0)

        is_nsfw = nsfw_score > 0.65  # Seuil que tu peux ajuster

        return JSONResponse({
            "is_nsfw": is_nsfw,
            "nsfw_score": round(nsfw_score, 4),
            "safe_score": round(safe_score, 4),
            "label": "nsfw" if is_nsfw else "safe",
            "all_results": results
        })

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))