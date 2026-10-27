from fastapi import Depends, FastAPI, File, Header, HTTPException, UploadFile
from PIL import Image, ImageOps, UnidentifiedImageError
import asyncio
import io
import os
import secrets
from transformers import pipeline
import torch

app = FastAPI(title="NSFW Moderation Service")

# Prevent Decompression Bomb DoS and enforce maximum pixel boundary
Image.MAX_IMAGE_PIXELS = 20_000_000

MODERATION_API_KEY = os.environ.get("MODERATION_API_KEY", "")


async def verify_api_key(x_api_key: str | None = Header(default=None, alias="X-API-Key")):
    if (
        not MODERATION_API_KEY
        or not x_api_key
        or not secrets.compare_digest(x_api_key, MODERATION_API_KEY)
    ):
        raise HTTPException(status_code=401, detail="Unauthorized")

# Charger le modèle une seule fois au démarrage
print("Loading Falconsai model...")
classifier = pipeline(
    "image-classification",
    model="Falconsai/nsfw_image_detection",
    device=0 if torch.cuda.is_available() else -1
)
print("Model loaded successfully!")


def load_image(data: bytes | bytearray) -> Image.Image:
    with Image.open(io.BytesIO(data)) as img:
        width, height = img.size
        if width * height > 20_000_000:
            raise HTTPException(status_code=400, detail="Image exceeds maximum allowed dimensions")
        transposed = ImageOps.exif_transpose(img)
        source = transposed if transposed is not None else img
        rgb_image = source.convert("RGB")
        if transposed is not None and transposed is not img:
            transposed.close()
        return rgb_image

@app.post("/moderate")
async def moderate_image(
    file: UploadFile = File(...),
    _: None = Depends(verify_api_key),
):
    if file.content_type is None or not file.content_type.startswith("image/"):
        raise HTTPException(status_code=400, detail="File must be an image")

    if file.size and file.size > 52428800:
        raise HTTPException(status_code=400, detail="File exceeds maximum allowed size (50MB)")

    image = None
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

    except Image.DecompressionBombError as e:
        print(f"Decompression bomb risk: {e}")
        raise HTTPException(status_code=400, detail="Image exceeds maximum allowed dimensions")
    except UnidentifiedImageError as e:
        print(f"Invalid image data: {e}")
        raise HTTPException(status_code=400, detail="Invalid or corrupted image file")
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error during moderation: {e}")
        raise HTTPException(status_code=500, detail="Internal server error during moderation")
    finally:
        if isinstance(image, Image.Image):
            image.close()
