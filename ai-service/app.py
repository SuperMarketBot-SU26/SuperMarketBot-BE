import base64
import cv2
import numpy as np
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from deepface import DeepFace

app = FastAPI(title="SmartMarketBot AI Face Service")

class ImageRequest(BaseModel):
    image_base64: str

def decode_base64_image(image_b64: str) -> np.ndarray:
    try:
        if "," in image_b64:
            image_b64 = image_b64.split(",")[1]
        img_bytes = base64.b64decode(image_b64)
        np_arr = np.frombuffer(img_bytes, np.uint8)
        img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
        if img is None:
            raise ValueError("Failed to decode image")
        return img
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Invalid image data: {str(e)}")

@app.post("/extract-vector")
def extract_vector(request: ImageRequest):
    try:
        img = decode_base64_image(request.image_base64)
        # Extract face vector using Facenet (returns a 128-dimensional vector, matching DB schema)
        representations = DeepFace.represent(
            img_path=img,
            model_name="Facenet",
            enforce_detection=True,
            detector_backend="opencv"
        )
        if not representations:
            raise HTTPException(status_code=400, detail="No face detected in the image")
        
        vector = representations[0]["embedding"]
        return {
            "status": "success",
            "face_vector": vector
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/verify")
def verify(request: ImageRequest):
    # Fallback to C# DB scan, so we return a failed response
    # to let the C# backend handle the matching via Cosine Similarity in the DB.
    return {
        "status": "failed",
        "message": "Please use /extract-vector for database matching"
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
