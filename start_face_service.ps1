# ============================================================
# Script khởi động Face Recognition Service
# Chạy: PowerShell -ExecutionPolicy Bypass -File start_face_service.ps1
# ============================================================

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$venvPython  = Join-Path $projectRoot ".venv\Scripts\uvicorn.exe"
$serviceDir  = Join-Path $projectRoot "face_service"

# Ẩn log rác của TensorFlow
$env:TF_ENABLE_ONEDNN_OPTS  = "0"
$env:TF_CPP_MIN_LOG_LEVEL   = "2"
$env:PYTHONIOENCODING        = "utf-8"

# Khởi động SQL LocalDB nếu chưa chạy
$state = (sqllocaldb info MSSQLLocalDB | Select-String "State").ToString()
if ($state -notmatch "Running") {
    Write-Host "[DB] Khoi dong SQL LocalDB..." -ForegroundColor Yellow
    sqllocaldb start MSSQLLocalDB | Out-Null
}
Write-Host "[DB] SQL LocalDB san sang." -ForegroundColor Green

# Khởi động FastAPI
Write-Host "[API] Khoi dong Face Service tai http://localhost:8000 ..." -ForegroundColor Cyan
Set-Location $serviceDir
& $venvPython main:app --host 0.0.0.0 --port 8000 --reload
