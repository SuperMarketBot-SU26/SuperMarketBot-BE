"""
Test script cho Face Login System
  - Bước 1: Chụp ảnh từ webcam và đăng ký mặt vào hệ thống  (register)
  - Bước 2: Chụp lại và test luồng n8n webhook đầy đủ         (login)

Chạy: py -3.11 test_face.py   (hoặc python test_face.py nếu mặc định là 3.11)
"""

import cv2
import base64
import json
import requests
import sys

FACE_SERVICE = "http://localhost:8000"
# Production C# API Face Login:
CS_API_URL = "http://localhost:5000/api/auth/face-login"


def fetch_members_summary() -> dict | None:
    try:
        r = requests.get(f"{FACE_SERVICE}/members", timeout=10)
        if r.status_code != 200:
            return None
        return r.json()
    except (requests.ConnectionError, requests.Timeout):
        return None


def print_registered_faces_summary(verbose: bool = False) -> None:
    """In số thành viên có vector khuôn mặt (để verify)."""
    data = fetch_members_summary()
    if not data:
        print(
            "  [!] Không lấy được danh sách từ Face Service — hãy bật service (port 8000) và thử lại."
        )
        return
    n_vec = data.get("count_with_face_vector", 0)
    n_tot = data.get("total_members", 0)
    print(f"  • Tổng bản ghi Members: {n_tot}")
    print(f"  • Đã có vector khuôn mặt (đủ để nhận diện): {n_vec}")
    if verbose:
        members = data.get("members") or []
        if not members:
            print("  • (Không có dòng nào trong bảng Members)")
            return
        print("  — Danh sách —")
        for m in members:
            fid = m.get("member_id")
            name = m.get("full_name", "?")
            phone = m.get("phone_number", "")
            hv = "có vector" if m.get("has_face_vector") else "chưa vector"
            print(f"      ID={fid}  |  {name}  |  {phone}  |  {hv}")


def capture_face_base64(window_title: str) -> str:
    """Mở webcam, hiển thị preview, nhấn SPACE để chụp."""
    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        sys.exit("[LỖI] Không mở được webcam. Kiểm tra kết nối camera.")

    print(f"\n>>> {window_title}")
    print("    Nhìn thẳng vào camera  →  nhấn [SPACE] để chụp  |  [Q] để thoát\n")

    frame_to_encode = None
    while True:
        ret, frame = cap.read()
        if not ret:
            break
        display = frame.copy()
        cv2.putText(
            display,
            "SPACE = chup  |  Q = thoat",
            (10, 30),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (0, 255, 0),
            2,
        )
        cv2.imshow(window_title, display)

        key = cv2.waitKey(1) & 0xFF
        if key == ord(" "):
            frame_to_encode = frame
            break
        elif key == ord("q"):
            cap.release()
            cv2.destroyAllWindows()
            sys.exit("Đã thoát.")

    cap.release()
    cv2.destroyAllWindows()

    _, buf = cv2.imencode(".jpg", frame_to_encode)
    return base64.b64encode(buf).decode("utf-8")


def step_register():
    print("=" * 55)
    print("  BƯỚC 1: ĐĂNG KÝ KHUÔN MẶT")
    print("=" * 55)

    full_name = input("Nhập tên của bạn   : ").strip() or "Test User"
    phone_number = input("Nhập số điện thoại : ").strip() or "0900000000"

    b64 = capture_face_base64("Dang ky khuon mat — nhan SPACE de chup")

    payload = {
        "full_name": full_name,
        "phone_number": phone_number,
        "image_base64": b64,
    }

    print("\n[→] Gửi ảnh đến Face Service /register ...")
    try:
        resp = requests.post(f"{FACE_SERVICE}/register", json=payload, timeout=30)
        data = resp.json()
        print(f"[✓] Kết quả: {json.dumps(data, ensure_ascii=False, indent=2)}")
        if data.get("status") == "success":
            print(f"\n    MemberID = {data['member_id']}  ← ghi lại nếu cần\n")
        print("\n[Cập nhật sau đăng ký]")
        print_registered_faces_summary(verbose=False)
        return data
    except requests.ConnectionError:
        sys.exit(
            f"[LỖI] Không kết nối được {FACE_SERVICE}. Hãy chạy Face Service trước (README — py -3.11 -m uvicorn)."
        )


def step_login():
    print("=" * 55)
    print("  BƯỚC 2: TEST FACE LOGIN (qua C# Backend API)")
    print("=" * 55)

    print("\n[Trước khi login]")
    print_registered_faces_summary(verbose=False)

    b64 = capture_face_base64("Face Login — nhan SPACE de chup")

    payload = {"imageBase64": b64}

    print(f"\n[→] Gửi ảnh đến C# API {CS_API_URL} ...")
    try:
        resp = requests.post(CS_API_URL, json=payload, timeout=60)
        print(f"    HTTP Status: {resp.status_code}")
        
        if resp.status_code == 401:
            print("[x] Đăng nhập thất bại: Không nhận diện được khuôn mặt.")
            try:
                print(json.dumps(resp.json(), ensure_ascii=False, indent=2))
            except Exception:
                print(resp.text)
            return

        data = resp.json()
        print(f"\n[✓] Phản hồi thành công từ C# Backend:\n{json.dumps(data, ensure_ascii=False, indent=2)}")

    except requests.ConnectionError:
        sys.exit(
            f"[LỖI] Không kết nối được C# Backend tại {CS_API_URL}.\n"
            "      Hãy đảm bảo C# Web API đang chạy trên cổng 5000."
        )
    except Exception as e:
        body = ""
        try:
            body = resp.text[:500]
        except Exception:
            pass
        print(f"[LỖI] {e}\n      Response raw: {body}")


def step_login_direct():
    """Test trực tiếp Python service (không qua n8n) — dùng khi n8n chưa sẵn sàng."""
    print("=" * 55)
    print("  BƯỚC 2b: TEST TRỰC TIẾP (bỏ qua n8n)")
    print("=" * 55)

    print("\n[Trước khi verify]")
    print_registered_faces_summary(verbose=False)

    b64 = capture_face_base64("Verify truc tiep — nhan SPACE de chup")
    payload = {"image_base64": b64}

    print(f"\n[→] Gửi ảnh đến {FACE_SERVICE}/verify ...")
    try:
        resp = requests.post(f"{FACE_SERVICE}/verify", json=payload, timeout=30)
        data = resp.json()
        print(f"\n[✓] Kết quả:\n{json.dumps(data, ensure_ascii=False, indent=2)}")
    except requests.ConnectionError:
        sys.exit(f"[LỖI] Không kết nối được {FACE_SERVICE}.")


def step_delete_member():
    print("=" * 55)
    print("  XÓA THÀNH VIÊN (theo MemberID)")
    print("=" * 55)
    print_registered_faces_summary(verbose=True)
    mid_s = input("\nNhập MemberID cần xóa (số nguyên): ").strip()
    if not mid_s.isdigit():
        print("MemberID không hợp lệ.")
        return
    member_id = int(mid_s)
    confirm = input(
        f'Xác nhận xóa MemberID={member_id}? Gõ chính xác "xoa" rồi Enter: '
    ).strip()
    if confirm.lower() != "xoa":
        print("Đã hủy.")
        return
    try:
        r = requests.delete(f"{FACE_SERVICE}/members/{member_id}", timeout=15)
        if r.status_code == 200:
            print(json.dumps(r.json(), ensure_ascii=False, indent=2))
        else:
            print(f"HTTP {r.status_code}: {r.text}")
    except requests.ConnectionError:
        sys.exit(f"[LỖI] Không kết nối được {FACE_SERVICE}.")
    print("\n[Sau khi xóa]")
    print_registered_faces_summary(verbose=False)


if __name__ == "__main__":
    print("\n╔══════════════════════════════════════════════════════╗")
    print("║         FACE LOGIN SYSTEM — TEST TOOL               ║")
    print("╚══════════════════════════════════════════════════════╝")
    print("\nThống kê đăng ký khuôn mặt (Face Service):")
    print_registered_faces_summary(verbose=False)
    print()
    print("Chọn thao tác:")
    print("  [1] Đăng ký mặt mới (register)")
    print("  [2] Test login qua C# Backend API (đầy đủ C# + Python)")
    print("  [3] Test login trực tiếp Python API (bỏ qua C#)")
    print("  [4] Đăng ký rồi test login luôn (1 → 3)")
    print("  [5] Liệt kê chi tiết thành viên (đọc từ Face Service)")
    print("  [6] Xóa thành viên theo MemberID (+ lịch sử mua, file ảnh)")
    print()

    choice = input("Nhập lựa chọn [1-6]: ").strip()

    if choice == "1":
        step_register()
    elif choice == "2":
        step_login()
    elif choice == "3":
        step_login_direct()
    elif choice == "4":
        step_register()
        input("\nNhấn Enter để tiếp tục test login...")
        step_login_direct()
    elif choice == "5":
        print_registered_faces_summary(verbose=True)
    elif choice == "6":
        step_delete_member()
    else:
        print("Lựa chọn không hợp lệ.")
