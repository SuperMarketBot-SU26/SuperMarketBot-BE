import base64
import cv2
import numpy as np
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from deepface import DeepFace

app = FastAPI(title="SmartMarketBot AI Face Service")

@app.on_event("startup")
def warmup_models():
    print("[AI Service] Warming up DeepFace models (Facenet & OpenCV)...")
    try:
        dummy_img = np.zeros((100, 100, 3), dtype=np.uint8)
        DeepFace.represent(
            img_path=dummy_img,
            model_name="Facenet",
            enforce_detection=False,
            detector_backend="opencv"
        )
        print("[AI Service] Warmup completed successfully! Model Facenet is loaded in RAM.")
    except Exception as e:
        print(f"[AI Service] Warmup warning: {e}")

class ImageRequest(BaseModel):
    image_base64: str
    detector_backend: str = "opencv"

def decode_base64_image(image_b64: str) -> np.ndarray:
    try:
        if "," in image_b64:
            image_b64 = image_b64.split(",")[1]
        img_bytes = base64.b64decode(image_b64)
        np_arr = np.frombuffer(img_bytes, np.uint8)
        img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
        if img is None:
            raise ValueError("Failed to decode image")
        
        # Tối ưu hóa hiệu năng: Giảm kích thước ảnh lớn về tối đa 640px để tăng tốc độ nhận diện
        h, w = img.shape[:2]
        max_size = 640
        if max(h, w) > max_size:
            scale = max_size / max(h, w)
            img = cv2.resize(img, (int(w * scale), int(h * scale)))
            
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
            enforce_detection=True if request.detector_backend != "skip" else False,
            detector_backend=request.detector_backend
        )
        if not representations:
            return {
                "status": "failed",
                "message": "No face detected in the image",
                "face_vector": []
            }
        
        vector = representations[0]["embedding"]
        return {
            "status": "success",
            "face_vector": vector
        }
    except ValueError as ve:
        # DeepFace raises ValueError when enforce_detection=True and no face is detected
        return {
            "status": "failed",
            "message": f"No face detected: {str(ve)}",
            "face_vector": []
        }
    except HTTPException as he:
        raise he
    except Exception as e:
        import traceback
        traceback.print_exc()
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
