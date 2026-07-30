import sys
from unittest.mock import MagicMock, patch, AsyncMock
import pytest
import os
import io

mock_transformers = MagicMock()
sys.modules['transformers'] = mock_transformers
mock_torch = MagicMock()
sys.modules['torch'] = mock_torch

from PIL import Image
from fastapi.testclient import TestClient

import main
from main import app, verify_api_key

os.environ["MODERATION_API_KEY"] = "test-key"
main.MODERATION_API_KEY = "test-key"

app.dependency_overrides[verify_api_key] = lambda: None
client = TestClient(app)

def create_dummy_image_bytes():
    image = Image.new('RGB', (10, 10))
    img_byte_arr = io.BytesIO()
    image.save(img_byte_arr, format='JPEG')
    return img_byte_arr.getvalue()

def test_api_key_verification_valid():
    real_client = TestClient(app)
    app.dependency_overrides.clear()
    with patch("main.asyncio.to_thread", new_callable=AsyncMock) as mock_to_thread:
        mock_to_thread.return_value = [{"label": "normal", "score": 0.9}]
        response = real_client.post(
            "/moderate",
            headers={"X-API-Key": "test-key"},
            files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
        )
        assert response.status_code == 200
    app.dependency_overrides[verify_api_key] = lambda: None

def test_api_key_verification_invalid():
    real_client = TestClient(app)
    app.dependency_overrides.clear()
    response = real_client.post(
        "/moderate",
        headers={"X-API-Key": "wrong-key"},
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 401
    assert response.json()["detail"] == "Unauthorized"
    app.dependency_overrides[verify_api_key] = lambda: None

def test_api_key_verification_missing_header():
    real_client = TestClient(app)
    app.dependency_overrides.clear()
    response = real_client.post(
        "/moderate",
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 401
    assert response.json()["detail"] == "Unauthorized"
    app.dependency_overrides[verify_api_key] = lambda: None

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
    mock_to_thread.side_effect = Image.DecompressionBombError("Image size exceeds limit")

    response = client.post(
        "/moderate",
        files={"file": ("test.jpg", create_dummy_image_bytes(), "image/jpeg")}
    )
    assert response.status_code == 400
    assert response.json()["detail"] == "Image exceeds maximum allowed dimensions"
