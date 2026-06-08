from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, List
from deepface import DeepFace
from dotenv import load_dotenv
import base64
import numpy as np
import cv2
import os
import uuid
import database

load_dotenv()

app = FastAPI(title="Face Recognition Microservice", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
STORAGE_DIR = os.path.join(BASE_DIR, "..", "storage", "member_faces")
os.makedirs(STORAGE_DIR, exist_ok=True)

FACENET_THRESHOLD = 0.3


class VerifyRequest(BaseModel):
    image_base64: str
    bbox: Optional[List[int]] = None  # [ymin, xmin, ymax, xmax] chuẩn hóa 0-1000


class RegisterRequest(BaseModel):
    full_name: str
    phone_number: str
    image_base64: str
    bbox: Optional[List[int]] = None


def base64_to_image(base64_str: str) -> np.ndarray:
    if "," in base64_str:
        base64_str = base64_str.split(",")[1]
    try:
        img_data = base64.b64decode(base64_str)
        nparr = np.frombuffer(img_data, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if img is None:
            raise ValueError("Không thể decode ảnh")
        return img
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Lỗi format ảnh Base64: {str(e)}")


def crop_image(img: np.ndarray, bbox: List[int]) -> np.ndarray:
    """Cắt khuôn mặt từ ảnh dựa trên BBox chuẩn hóa 0-1000 (ymin, xmin, ymax, xmax)"""
    if not bbox or len(bbox) != 4:
        return img
    try:
        h, w = img.shape[:2]
        ymin, xmin, ymax, xmax = bbox
        y1 = max(0, int((ymin / 1000.0) * h))
        x1 = max(0, int((xmin / 1000.0) * w))
        y2 = min(h, int((ymax / 1000.0) * h))
        x2 = min(w, int((xmax / 1000.0) * w))
        cropped = img[y1:y2, x1:x2]
        return cropped if cropped.size > 0 else img
    except Exception as e:
        print(f"Lỗi crop ảnh: {e}")
        return img


def extract_face_vector(img: np.ndarray) -> list:
    try:
        result = DeepFace.represent(
            img_path=img,
            model_name="Facenet",
            enforce_detection=False,
        )
        return result[0]["embedding"]
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi trích xuất vector: {str(e)}")


def cosine_similarity(vec1: list, vec2: list) -> float:
    a = np.array(vec1)
    b = np.array(vec2)
    norm = np.linalg.norm(a) * np.linalg.norm(b)
    if norm == 0:
        return 0.0
    return float(np.dot(a, b) / norm)


@app.get("/health")
def health_check():
    return {"status": "ok", "service": "face-recognition"}


@app.get("/members")
def list_members():
    """Đếm và liệt kê thành viên có vector khuôn mặt (phục vụ test tool)."""
    try:
        summary = database.list_members_brief()
        count_vec = database.count_members_with_face_vector()
        return {
            "status": "success",
            "count_with_face_vector": count_vec,
            "total_members": len(summary),
            "members": summary,
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/members/{member_id}")
def remove_member(member_id: int):
    """Xóa thành viên (lịch sử mua + bản ghi + file ảnh face nếu có)."""
    result = database.delete_member(member_id)
    if result.get("deleted"):
        return {
            "status": "success",
            "member_id": member_id,
            "message": "Đã xóa thành viên và dữ liệu liên quan.",
        }
    reason = result.get("reason", "Không xóa được")
    code = 404 if isinstance(reason, str) and reason.startswith("Không có MemberID") else 400
    raise HTTPException(status_code=code, detail=reason)


@app.post("/register")
async def register_member(req: RegisterRequest):
    img = base64_to_image(req.image_base64)
    if req.bbox:
        img = crop_image(img, req.bbox)

    vector = extract_face_vector(img)

    filename = f"{uuid.uuid4().hex}.jpg"
    filepath = os.path.join(STORAGE_DIR, filename)
    cv2.imwrite(filepath, img)

    try:
        member_id = database.insert_member(
            full_name=req.full_name,
            phone_number=req.phone_number,
            face_path=filepath,
            face_vector=vector,
        )
        return {
            "status": "success",
            "member_id": member_id,
            "message": "Đăng ký khuôn mặt thành công",
        }
    except Exception as e:
        if os.path.exists(filepath):
            os.remove(filepath)
        raise HTTPException(status_code=500, detail=f"Lỗi lưu Database: {str(e)}")


@app.post("/verify")
async def verify_member(req: VerifyRequest):
    img = base64_to_image(req.image_base64)
    if req.bbox:
        img = crop_image(img, req.bbox)

    target_vector = extract_face_vector(img)

    try:
        all_members = database.get_all_members_vectors()
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi truy vấn Database: {str(e)}")

    if not all_members:
        return {"status": "failed", "message": "Chưa có dữ liệu khuôn mặt trong hệ thống"}

    best_match_id = None
    best_score = -1.0

    for member in all_members:
        score = cosine_similarity(target_vector, member["FaceVector"])
        if score > best_score:
            best_score = score
            best_match_id = member["MemberID"]

    if best_score >= FACENET_THRESHOLD:
        return {
            "status": "success",
            "member_id": best_match_id,
            "confidence_score": round(best_score, 4),
            "message": "Nhận diện thành công",
        }
    return {
        "status": "failed",
        "message": "Khuôn mặt không khớp với bất kỳ ai",
        "highest_score": round(best_score, 4),
    }


@app.post("/extract-vector")
async def extract_vector(req: VerifyRequest):
    img = base64_to_image(req.image_base64)
    if req.bbox:
        img = crop_image(img, req.bbox)
    vector = extract_face_vector(img)
    return {"status": "success", "face_vector": vector}


@app.get("/user-info/{member_id}")
async def get_user_info(member_id: int):
    try:
        user_data = database.get_user_history(member_id)
        if user_data:
            return {"status": "success", "data": user_data}
        return {"status": "failed", "message": "Không tìm thấy thông tin thành viên"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
