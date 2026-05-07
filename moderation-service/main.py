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
        
        # === LOG IMPORTANT pour debug ===
        print("=== RAW MODEL OUTPUT ===")
        print(results)
        print("========================")

        nsfw_score = next(
            (r["score"] for r in results if r["label"].lower() == "nsfw"),
            0.0
        )

        return {
            "is_nsfw": nsfw_score > 0.60,
            "nsfw_score": round(nsfw_score, 4),
            "safe_score": round(1.0 - nsfw_score, 4),
            "label": results[0]["label"]
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))