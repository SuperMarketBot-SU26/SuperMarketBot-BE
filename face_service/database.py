import pyodbc
import json
import os
from dotenv import load_dotenv

load_dotenv()

CONNECTION_STRING = os.getenv(
    "DB_CONNECTION_STRING",
    (
        r"DRIVER={ODBC Driver 17 for SQL Server};"
        r"SERVER=(localdb)\MSSQLLocalDB;"
        r"DATABASE=SuperMarketBot;"
        r"Trusted_Connection=yes;"
        r"Encrypt=no;"
    ),
)


def get_db_connection() -> pyodbc.Connection:
    return pyodbc.connect(CONNECTION_STRING)


def insert_member(full_name: str, phone_number: str, face_path: str, face_vector: list) -> int:
    """Lưu thành viên mới, trả về MemberID vừa tạo."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        vector_json = json.dumps(face_vector)
        query = """
        INSERT INTO Members (FullName, PhoneNumber, FacePath, FaceVector)
        OUTPUT INSERTED.MemberID
        VALUES (?, ?, ?, ?)
        """
        cursor.execute(query, (full_name, phone_number, face_path, vector_json))
        member_id = cursor.fetchone()[0]
        conn.commit()
        return member_id
    finally:
        cursor.close()
        conn.close()


def get_all_members_vectors() -> list:
    """Trả về danh sách {MemberID, FaceVector} của tất cả thành viên có vector."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute("SELECT MemberID, FaceVector FROM Members WHERE FaceVector IS NOT NULL")
        members = []
        for row in cursor.fetchall():
            try:
                members.append({
                    "MemberID": row.MemberID,
                    "FaceVector": json.loads(row.FaceVector),
                })
            except Exception as e:
                print(f"Bỏ qua MemberID {row.MemberID} do lỗi parse vector: {e}")
        return members
    finally:
        cursor.close()
        conn.close()


def count_members_with_face_vector() -> int:
    """Số thành viên đã có vector khuôn mặt (đủ để verify)."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute(
            "SELECT COUNT(*) FROM Members WHERE FaceVector IS NOT NULL AND LEN(FaceVector) > 2"
        )
        return int(cursor.fetchone()[0])
    finally:
        cursor.close()
        conn.close()


def list_members_brief() -> list[dict]:
    """Liệt kê thành viên: MemberID, tên, SĐT, có vector hay không."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute(
            """
            SELECT MemberID, FullName, PhoneNumber,
                   CASE WHEN FaceVector IS NOT NULL AND LEN(FaceVector) > 2 THEN 1 ELSE 0 END AS HasVector,
                   FacePath
            FROM Members
            ORDER BY MemberID
            """
        )
        rows = cursor.fetchall()
        return [
            {
                "member_id": int(r.MemberID),
                "full_name": r.FullName,
                "phone_number": r.PhoneNumber or "",
                "has_face_vector": bool(r.HasVector),
                "face_path": r.FacePath,
            }
            for r in rows
        ]
    finally:
        cursor.close()
        conn.close()


def delete_member(member_id: int) -> dict:
    """
    Xóa thành viên: PurchaseHistory (nếu bảng có) → file ảnh → Members.
    DB cũ chỉ có Members thì vẫn xóa được.
    Trả về {'deleted': bool, 'reason': ...}
    """
    conn = get_db_connection()
    cursor = conn.cursor()
    face_path_local = None
    try:
        cursor.execute(
            "SELECT MemberID, FacePath FROM dbo.Members WHERE MemberID = ?",
            (member_id,),
        )
        row = cursor.fetchone()
        if not row:
            return {"deleted": False, "reason": "Không có MemberID này."}
        face_path_local = row.FacePath
        try:
            cursor.execute(
                "DELETE FROM dbo.PurchaseHistory WHERE MemberID = ?",
                (member_id,),
            )
        except pyodbc.Error as e:
            if "42S02" not in "".join(str(x) for x in e.args):
                raise
        cursor.execute("DELETE FROM dbo.Members WHERE MemberID = ?", (member_id,))
        conn.commit()

        if face_path_local and os.path.isfile(face_path_local):
            try:
                os.remove(face_path_local)
            except OSError:
                pass
        return {"deleted": True, "member_id": member_id}
    except Exception as e:
        conn.rollback()
        return {"deleted": False, "reason": str(e)}
    finally:
        cursor.close()
        conn.close()


def get_user_history(member_id: int) -> dict | None:
    """Truy vấn tên hội viên và top 5 sản phẩm mua nhiều nhất."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        query = """
        SELECT
            m.FullName AS MemberName,
            ISNULL(
                (
                    SELECT STRING_AGG(sub.ProductName, ', ') WITHIN GROUP (ORDER BY sub.PurchaseCount DESC)
                    FROM (
                        SELECT TOP 5 ProductName, COUNT(*) AS PurchaseCount
                        FROM PurchaseHistory
                        WHERE MemberID = ?
                        GROUP BY ProductName
                        ORDER BY PurchaseCount DESC
                    ) sub
                ),
                ''
            ) AS TopProducts
        FROM Members m
        WHERE m.MemberID = ?
        """
        cursor.execute(query, (member_id, member_id))
        row = cursor.fetchone()
        if row:
            return {"MemberName": row.MemberName, "TopProducts": row.TopProducts or ""}
        return None
    except Exception as e:
        print(f"Lỗi SQL get_user_history (full query): {e}")
        try:
            cursor.execute("SELECT FullName FROM Members WHERE MemberID = ?", (member_id,))
            basic_row = cursor.fetchone()
            if basic_row:
                return {"MemberName": basic_row.FullName, "TopProducts": ""}
        except Exception as fallback_err:
            print(f"Lỗi SQL fallback: {fallback_err}")
        return None
    finally:
        cursor.close()
        conn.close()
