# SmartMarketBot — Backend (.NET 10 & Python AI Microservice)

High-performance Backend system for **Smart Supermarket System** (Autonomous Navigation Robot, Dynamic Ad Campaigns, Personalized Face Recognition & AI Meal Recommendations).

- **Architecture:** Clean Architecture 4-Layer (.NET 10 + EF Core 10 + Azure SQL)
- **AI Microservice:** Python FastAPI (`ai-service`) with DeepFace (Facenet + OpenCV) for 128D Face Embedding Extraction & Gemini 2.5 Flash for personalized greetings
- **Realtime & IoT:** SignalR Hubs (`/hubs/robot`, `/hubs/staff`, `/hubs/member`) + MQTT HiveMQ Broker

---

## 🚀 Quick Start (Docker Compose)

```bash
# Run full stack (API + Python AI Service + Mosquitto Broker)
docker compose up -d --build

# Swagger / Scalar Docs: http://localhost:5000/scalar/v1 (or /swagger)
# OpenAPI JSON:        http://localhost:5000/openapi/v1.json
# Python AI Service:   http://localhost:8000
```

| Component | Port | Host / URL |
|---|---|---|
| ASP.NET Core API | 5000 | `http://localhost:5000` |
| Python AI Service | 8000 | `http://localhost:8000` (Docker internal: `http://ai-service:8000`) |
| Azure SQL Database | 1433 | `supermarketbot.database.windows.net,1433` |
| HiveMQ Broker | 8883 | `60922debd474446a84747b871c4a8182.s1.eu.hivemq.cloud` |
| SignalR Hubs | 5000 | `/hubs/robot`, `/hubs/staff`, `/hubs/member` |

---

## 🛠️ Local Development (without Docker)

```bash
# Run Python AI Service locally
cd ai-service
pip install -r requirements.txt
python app.py

# Run .NET API locally
cd ..
dotnet run --project src/SmartMarketBot.API
```

---

## 🏗️ Architecture & Solution Layout

```
SuperMarketBot-BE/
├── ai-service/                Python FastAPI Face Recognition Microservice
│   ├── app.py                 DeepFace (Facenet) vector extraction + model warmup
│   ├── requirements.txt       FastAPI, OpenCV, DeepFace, TensorFlow
│   └── Dockerfile             Pre-downloads model weights during docker build
├── src/
│   ├── SmartMarketBot.Domain/         Entities (SNAKE_CASE mappings), Enums, Value Objects
│   ├── SmartMarketBot.Application/    Interfaces, DTOs, Business Services, Dijkstra Routing
│   ├── SmartMarketBot.Infrastructure/ AppDbContext (EF Core Retry), FaceAiService, GeminiService, MQTT
│   └── SmartMarketBot.API/            Controllers, SignalR Hubs, Kestrel Config, GlobalExceptionMiddleware
├── docker-compose.yml         Multi-container orchestration
└── Dockerfile                 Multi-stage build for ASP.NET Core 10
```

---

## 🔑 Core Features & API Map

### 1. Authentication & Face Login (`/api/auth`)
- `POST /api/auth/login` — Standard Email/Password login.
- `POST /api/auth/face-login` — High-speed face recognition. Calls Python `ai-service` (`/extract-vector`), computes Cosine Similarity (threshold 0.60), fetches top purchases, and generates personalized greetings via Gemini AI.
- `POST /api/auth/register-face` — Register/extract 128D face vector for active member.

### 2. Autonomous Navigation & Pathfinding
- `Dijkstra Algorithm` in Application layer for shortest aisle path calculation.
- MQTT topics for telemetry & robot position tracking: `smartmarketbot/robot/{robotId}/telemetry`.

### 3. Smart Advertising & Dynamic Playlist
- Ad Campaign matching based on robot position, aisle semantic targets, and sponsor budgets.

---

## 💡 AI Developer & Vibe Coding Guidelines

When extending or maintaining this repository with AI coding assistants:

1. **Clean Layer Dependencies:**
   `Domain` (No external dependencies) ⬅ `Application` ⬅ `Infrastructure` ⬅ `API`.
2. **EF Core SQL Transient Faults:**
   Azure SQL transient retries are enabled via `EnableRetryOnFailure` in `DependencyInjection.cs`. Always use `async/await` and pass `CancellationToken` for DB queries.
3. **Kestrel Request Body Limits:**
   Kestrel `MinRequestBodyDataRate` checks are disabled in `Program.cs` and `GlobalExceptionMiddleware.cs` to prevent payload upload timeouts over proxy tunnels (ngrok/LTE).
4. **Python AI Service Warmup:**
   `ai-service/app.py` preloads `Facenet` and `OpenCV` models during `@app.on_event("startup")`. Any model additions must be pre-warmed in `warmup_models()` to avoid cold-start delays.

---

## 📜 License & Project Info
Developed for SmartMarketBot Capstone Project.
