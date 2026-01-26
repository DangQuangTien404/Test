import pytest
import requests
import json
import urllib3
import uuid

# --- CẤU HÌNH ---
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
BASE_URL = "https://localhost:7025/api" # Check lại Port của bạn (7025 hoặc 5213)

# Biến lưu trữ dữ liệu chạy xuyên suốt
store = {
    "manager_token": None, "manager_id": None,
    "annotator_token": None, "annotator_id": None,
    "reviewer_token": None, "reviewer_id": None,
    "project_id": None,
    "label_id": None,
    "data_item_id": None,
    "assignment_id": None
}

# Tài khoản test (Tạo mới mỗi lần chạy để sạch sẽ)
RANDOM_CODE = uuid.uuid4().hex[:6]
MANAGER_ACC = {"name": "Test Manager", "email": f"manager_{RANDOM_CODE}@test.com", "password": "Password123!", "role": "Manager"}
ANNOTATOR_ACC = {"name": "Test Annotator", "email": f"anno_{RANDOM_CODE}@test.com", "password": "Password123!", "role": "Annotator"}
REVIEWER_ACC = {"name": "Test Reviewer", "email": f"review_{RANDOM_CODE}@test.com", "password": "Password123!", "role": "Reviewer"}

# Hàm helper request
def req(method, endpoint, token=None, data=None):
    headers = {"Authorization": f"Bearer {token}"} if token else {}
    url = f"{BASE_URL}/{endpoint}"
    
    if method == "POST": return requests.post(url, json=data, headers=headers, verify=False)
    if method == "PUT": return requests.put(url, json=data, headers=headers, verify=False)
    if method == "DELETE": return requests.delete(url, headers=headers, verify=False)
    if method == "GET": return requests.get(url, headers=headers, verify=False)

# =================================================================================
# 1. AUTH MODULE & USER PROFILE
# =================================================================================
def test_01_register_users():
    """Đăng ký 3 tài khoản: Manager, Annotator, Reviewer"""
    for acc in [MANAGER_ACC, ANNOTATOR_ACC, REVIEWER_ACC]:
        resp = req("POST", "Auth/register", data={"fullName": acc["name"], "email": acc["email"], "password": acc["password"], "role": acc["role"]})
        assert resp.status_code == 200, f"Register failed for {acc['role']}: {resp.text}"
    print("\n✅ 1. Registered 3 Users")

def test_02_login_manager():
    """Login Manager & Lấy Token"""
    resp = req("POST", "Auth/login", data={"email": MANAGER_ACC["email"], "password": MANAGER_ACC["password"]})
    assert resp.status_code == 200
    store["manager_token"] = resp.json().get("accessToken")
    print("✅ 2. Manager Logged In")

def test_03_update_profile_fix_check():
    """Test API vừa fix: Manager tự sửa Profile của mình"""
    payload = {"fullName": "Manager Updated Name", "phoneNumber": "0123456789"}
    # Gọi API PUT /api/User/profile (Cái bạn vừa thêm)
    resp = req("PUT", "User/profile", token=store["manager_token"], data=payload)
    assert resp.status_code == 200, f"Fix chưa chạy! Lỗi: {resp.text}"
    
    # Check lại xem tên đổi chưa
    check = req("GET", "User/profile", token=store["manager_token"])
    assert check.json()["fullName"] == "Manager Updated Name"
    store["manager_id"] = check.json()["id"]
    print("✅ 3. Update Profile (Bug Fix Verified!)")

# =================================================================================
# 2. PROJECT MODULE (CRUD)
# =================================================================================
def test_04_create_project():
    """Manager tạo dự án mới"""
    payload = {
        "name": f"Project {RANDOM_CODE}",
        "description": "Full Flow Test",
        "pricePerLabel": 100,
        "totalBudget": 5000,
        "deadline": "2026-12-31T00:00:00",
        "labelClasses": [{"name": "Car", "color": "#FF0000"}] # Tạo sẵn 1 label
    }
    resp = req("POST", "Project", token=store["manager_token"], data=payload)
    assert resp.status_code == 200
    store["project_id"] = resp.json().get("projectId")
    print(f"✅ 4. Project Created (ID: {store['project_id']})")

def test_05_update_project():
    """Manager sửa thông tin dự án"""
    payload = {
        "name": f"Project {RANDOM_CODE} Edited",
        "description": "Updated Description",
        "pricePerLabel": 150,
        "totalBudget": 6000,
        "deadline": "2027-01-01T00:00:00"
    }
    resp = req("PUT", f"Project/{store['project_id']}", token=store["manager_token"], data=payload)
    assert resp.status_code == 200
    print("✅ 5. Project Updated")

def test_06_add_label_to_project():
    """Thêm nhãn mới vào dự án (Label Controller)"""
    payload = {"projectId": store["project_id"], "name": "Bike", "color": "#00FF00"}
    resp = req("POST", "Label", token=store["manager_token"], data=payload)
    assert resp.status_code == 200
    store["label_id"] = resp.json()["id"]
    print(f"✅ 6. Label Added (ID: {store['label_id']})")

def test_07_import_data():
    """Import dữ liệu (ảnh) vào dự án"""
    payload = {"storageUrls": ["https://dummyimage.com/100x100/000/fff", "https://dummyimage.com/200x200/000/fff"]}
    resp = req("POST", f"Project/{store['project_id']}/import-data", token=store["manager_token"], data=payload)
    assert resp.status_code == 200
    print("✅ 7. Data Imported")

# =================================================================================
# 3. TASK ASSIGNMENT (MANAGER)
# =================================================================================
def test_08_assign_task():
    """Giao việc cho Annotator"""
    # Login Annotator để lấy ID
    login_resp = req("POST", "Auth/login", data={"email": ANNOTATOR_ACC["email"], "password": ANNOTATOR_ACC["password"]})
    store["annotator_token"] = login_resp.json().get("accessToken")
    
    # Lấy ID Annotator
    profile = req("GET", "User/profile", token=store["annotator_token"])
    store["annotator_id"] = profile.json()["id"]

    # Manager giao 1 task
    payload = {"projectId": store["project_id"], "annotatorId": store["annotator_id"], "quantity": 1}
    resp = req("POST", "Task/assign", token=store["manager_token"], data=payload)
    assert resp.status_code == 200
    print("✅ 8. Task Assigned")

# =================================================================================
# 4. ANNOTATOR WORKFLOW
# =================================================================================
def test_09_annotator_get_tasks():
    """Annotator lấy danh sách task"""
    resp = req("GET", f"Task/project/{store['project_id']}/images", token=store["annotator_token"])
    tasks = resp.json()
    assert len(tasks) > 0
    store["assignment_id"] = tasks[0]["id"]
    print(f"✅ 9. Annotator Received Task (ID: {store['assignment_id']})")

def test_10_annotator_save_draft():
    """Annotator lưu nháp"""
    payload = {"assignmentId": store["assignment_id"], "dataJSON": "{\"draft\": true}"}
    resp = req("POST", "Task/save-draft", token=store["annotator_token"], data=payload)
    assert resp.status_code == 200
    print("✅ 10. Draft Saved")

def test_11_annotator_submit():
    """Annotator nộp bài"""
    payload = {"assignmentId": store["assignment_id"], "dataJSON": "{\"final\": true, \"box\": [10,10,100,100]}"}
    resp = req("POST", "Task/submit", token=store["annotator_token"], data=payload)
    assert resp.status_code == 200
    print("✅ 11. Task Submitted")

# =================================================================================
# 5. REVIEWER WORKFLOW
# =================================================================================
def test_12_reviewer_approve():
    """Reviewer chấm bài (Approve)"""
    # Login Reviewer
    login_resp = req("POST", "Auth/login", data={"email": REVIEWER_ACC["email"], "password": REVIEWER_ACC["password"]})
    store["reviewer_token"] = login_resp.json().get("accessToken")

    # Duyệt bài
    payload = {"assignmentId": store["assignment_id"], "isApproved": True, "comment": "Perfect!"}
    resp = req("POST", "Review", token=store["reviewer_token"], data=payload)
    assert resp.status_code == 200
    print("✅ 12. Task Reviewed & Approved")

# =================================================================================
# 6. STATS & EXPORT
# =================================================================================
def test_13_check_stats():
    """Manager xem thống kê (Tiền độ phải tăng)"""
    resp = req("GET", f"Project/{store['project_id']}/stats", token=store["manager_token"])
    stats = resp.json()
    # Vì giao 1, làm 1, duyệt 1 -> completedItems phải >= 1
    assert stats["completedItems"] >= 1 or stats["approvedAssignments"] >= 1
    print("✅ 13. Stats Updated Successfully")

def test_14_export_data():
    """Manager xuất dữ liệu (Test fix lỗi Export)"""
    resp = req("GET", f"Project/{store['project_id']}/export", token=store["manager_token"])
    assert resp.status_code == 200
    assert len(resp.content) > 0
    print("✅ 14. Project Exported (JSON)")

# =================================================================================
# 7. CLEANUP (DELETE)
# =================================================================================
def test_15_delete_label():
    """Xóa nhãn"""
    resp = req("DELETE", f"Label/{store['label_id']}", token=store["manager_token"])
    assert resp.status_code == 200
    print("✅ 15. Label Deleted")

def test_16_delete_project():
    """Xóa dự án (Dọn dẹp)"""
    resp = req("DELETE", f"Project/{store['project_id']}", token=store["manager_token"])
    assert resp.status_code == 200
    print("✅ 16. Project Deleted")

    # Verify xóa chưa
    check = req("GET", f"Project/{store['project_id']}", token=store["manager_token"])
    assert check.status_code == 404
    print("✅ 17. Project Cleanup Verified")