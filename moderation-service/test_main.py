import sys
from unittest.mock import MagicMock, patch, AsyncMock
import pytest

mock_transformers = MagicMock()
sys.modules['transformers'] = mock_transformers
mock_torch = MagicMock()
sys.modules['torch'] = mock_torch

from fastapi.testclient import TestClient
import io
from PIL import Image

from main import app, verify_api_key
import main

app.dependency_overrides[verify_api_key] = lambda: None
client = TestClient(app)
client.headers.update({"X-API-Key": "test-key"})
import os
os.environ["MODERATION_API_KEY"] = "test-key"
import main
main.MODERATION_API_KEY = "test-key"

def create_dummy_image_bytes():
    image = Image.new('RGB', (10, 10))
    img_byte_arr = io.BytesIO()
    image.save(img_byte_arr, format='JPEG')
    return img_byte_arr.getvalue()

def test_non_image_content_type():
    response = client.post(
        "/moderate",
        files={"file": ("test.txt", b"hello world", "text/plain")}
    )
    assert response.status_code == 400
    assert response.json()["detail"] == "File must be an image"

def test_missing_content_type():
    response = client.post(
        "/moderate",
        files={"file": ("test.txt", b"hello world", None)}
    )
    assert response.status_code == 400
    assert response.json()["detail"] == "File must be an image"

@patch("main.asyncio.to_thread", new_callable=AsyncMock)
def test_successful_classification_nsfw(mock_to_thread):
    mock_to_thread.return_value = [
        {"label": "nsfw", "score": 0.9},
        {"label": "normal", "score": 0.1}
    ]

    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 200
    data = response.json()
    assert data["is_nsfw"] is True
    assert data["nsfw_score"] == 0.9
    assert data["safe_score"] == 0.1
    assert data["label"] == "nsfw"

@patch("main.asyncio.to_thread", new_callable=AsyncMock)
def test_successful_classification_safe(mock_to_thread):
    mock_to_thread.return_value = [
        {"label": "normal", "score": 0.8},
        {"label": "nsfw", "score": 0.2}
    ]

    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 200
    data = response.json()
    assert data["is_nsfw"] is False
    assert data["nsfw_score"] == 0.2
    assert data["safe_score"] == 0.8
    assert data["label"] == "normal"

@patch("main.asyncio.to_thread", new_callable=AsyncMock)
def test_internal_server_error(mock_to_thread):
    mock_to_thread.side_effect = Exception("Model failed")

    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 500
    assert response.json()["detail"] == "Internal server error during moderation"

@patch("starlette.datastructures.UploadFile.read", new_callable=AsyncMock)
def test_file_size_exceeds_content_length(mock_read):
    # Mock read to return slightly more than 50MB (52428800 bytes)
    mock_read.return_value = b"0" * 52428801
    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", b"small payload to save memory", "image/jpeg")}
    )
    assert response.status_code == 400
    assert response.json()["detail"] == "File exceeds maximum allowed size (50MB)"

from unittest.mock import PropertyMock

@patch("starlette.datastructures.UploadFile.size", new_callable=PropertyMock, create=True)
def test_file_size_exceeds_attribute(mock_size):
    mock_size.return_value = 52428801
    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", b"small payload", "image/jpeg")}
    )
    assert response.status_code == 400
    assert response.json()["detail"] == "File exceeds maximum allowed size (50MB)"

@patch("main.asyncio.to_thread", new_callable=AsyncMock)
def test_decompression_bomb(mock_to_thread):
    # Simulate a DecompressionBombError when load_image is called in to_thread
    mock_to_thread.side_effect = Image.DecompressionBombError("Image size exceeds limit")

    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 400
    assert response.json()["detail"] == "Image dimensions exceed maximum allowed size"
