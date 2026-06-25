from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import JSONResponse
from PIL import Image
import io
import asyncio
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


def load_image(data):
    return Image.open(io.BytesIO(data)).convert("RGB")

@app.post("/moderate")
async def moderate_image(file: UploadFile = File(...)):
    if file.content_type is None or not file.content_type.startswith("image/"):
        raise HTTPException(status_code=400, detail="File must be an image")

    if file.size and file.size > 52428800:
        raise HTTPException(status_code=400, detail="File exceeds maximum allowed size (50MB)")

    try:
        real_size = 0
        contents = bytearray()
        while True:
            chunk = await file.read(1024 * 1024)
            if not chunk:
                break
            real_size += len(chunk)
            if real_size > 52428800:
                raise HTTPException(status_code=400, detail="File exceeds maximum allowed size (50MB)")
            contents.extend(chunk)

        image = await asyncio.to_thread(load_image, contents)

        # Prédiction
        results = await asyncio.to_thread(classifier, image)

        # === LOG IMPORTANT pour debug ===
        print("=== RAW MODEL OUTPUT ===")
        print(results)
        print("========================")

        nsfw_score = next(
            (r["score"] for r in results if r["label"].lower() == "nsfw"),
            0.0
        )

        return {
            "is_nsfw": nsfw_score > 0.55,
            "nsfw_score": round(nsfw_score, 4),
            "safe_score": round(1.0 - nsfw_score, 4),
            "label": results[0]["label"]
        }

    except HTTPException:
        raise
    except Exception as e:
        print(f"Error during moderation: {e}")
        raise HTTPException(status_code=500, detail="Internal server error during moderation")
