# SmartMarketBot — Backend Developer Agent Guidelines (AGENTS.md)

This repository contains the Backend for **SmartMarketBot** (.NET 10 Clean Architecture + Python FastAPI AI Service + Docker + SignalR + MQTT).

---

## 🛠️ Technology Stack & Layering Rules

1. **Clean Architecture (4 Layers)**:
   - **`SmartMarketBot.Domain`**: Pure POCO Entities, Enums, Value Objects. **NO** dependencies on EF Core, ASP.NET, or external libraries.
   - **`SmartMarketBot.Application`**: Interfaces, DTOs, Services, Pathfinding Algorithms (Dijkstra). All business rules & validations live here.
   - **`SmartMarketBot.Infrastructure`**: EF Core `AppDbContext`, SQL Server connection, MQTT Broker (`MqttClientService`), Face AI HttpClient (`FaceAiService`), Gemini API (`GeminiService`).
   - **`SmartMarketBot.API`**: Thin Controllers, SignalR Hubs (`RobotHub`, `StaffHub`, `MemberHub`), Middlewares (`GlobalExceptionMiddleware`).

2. **Python AI Service (`ai-service/`)**:
   - FastAPI microservice running DeepFace (`Facenet` embedding extraction) + OpenCV face detection.
   - **Startup Warmup Mandatory:** Preload models in `@app.on_event("startup")` to eliminate cold-start inference delays.
   - **Image Resizing:** Downscale input frames to max width 640px before DeepFace processing to ensure < 100ms vector extraction.

---

## ⚡ Performance & Reliability Guidelines

- **EF Core Azure SQL Resiliency:** Always keep `EnableRetryOnFailure` enabled on `UseSqlServer` to handle Azure SQL Serverless wakeups gracefully.
- **Async & Cancellation Tokens:** All I/O calls (DB queries, HttpClient requests, File I/O) MUST be `async` and propagate `CancellationToken ct`.
- **Kestrel Timeout Protection:** Disable `MinRequestBodyDataRate` checks in `Program.cs` and `GlobalExceptionMiddleware.cs` so high-resolution camera frames sent over ngrok/LTE proxies do not trigger HTTP 500 payload timeouts.
- **Global Error Handling:** Do NOT wrap controllers in ad-hoc try-catches unless specific domain logic requires it. Let `GlobalExceptionMiddleware` catch and format errors cleanly.

---

## 📦 Recommended Skills & Git Conventions for AI Assistants

### 1. Git Commit Message Standards (Conventional Commits)
- `feat(auth)`: Add new authentication endpoint or token logic.
- `fix(ai)`: Resolve model loading delay or payload timeout in face service.
- `refactor(db)`: Optimize EF Core query with `.AsNoTracking()` or explicit projection.
- `docs`: Update API specs or README.

### 2. Essential AI Agent Skills for this Repo
- **EF Core LINQ Optimizer:** Detect N+1 query patterns, enforce `.AsNoTracking()` for read-only endpoints, ensure `.Include()` navigation properties are explicit.
- **FastAPI / OpenCV Memory Guard:** Ensure Python handlers do not leak OpenCV Mat buffers or TensorFlow GPU/RAM tensors.
- **API Spec Validator:** Ensure OpenAPI schema compatibility for Scalar API documentation (`http://localhost:5000/scalar/v1`).
