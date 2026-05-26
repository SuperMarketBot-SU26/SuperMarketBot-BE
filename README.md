# SmartMarketBot Backend

## Quick start

1. Start dependencies:
   - `docker compose up -d`
2. Build solution:
   - `dotnet build SmartMarketBot.sln`
3. Run API:
   - `dotnet run --project src/SmartMarketBot.API/SmartMarketBot.API.csproj`

The API runs on `http://localhost:5000` in Development and exposes SignalR at `/hubs/robot`.
