# SmartMarketBot — Backend (AGENTS.md)

Ban dang trong repo **BE**. Quy tac AI: `.cursor/rules/*.mdc`

## Stack

- .NET 10, Clean Architecture 4 layer
- EF Core 10 + SQL Server (`db/database.sql`)
- SignalR, MQTTnet, JWT

## Chay nhanh

```bash
docker compose up -d
dotnet run --project src/SmartMarketBot.API/SmartMarketBot.API.csproj
```

Swagger: `http://localhost:5000/swagger`

## Khong thuoc pham vi mac dinh

FE, Android, `SuperMarketBot-AI` (Python/n8n) — chi sua khi user yeu cau ro.
