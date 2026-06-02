# SmartMarketBot API — chạy trong Docker cùng SQL + Mosquitto
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SmartMarketBot.sln ./
COPY src/SmartMarketBot.Domain/SmartMarketBot.Domain.csproj src/SmartMarketBot.Domain/
COPY src/SmartMarketBot.Application/SmartMarketBot.Application.csproj src/SmartMarketBot.Application/
COPY src/SmartMarketBot.Infrastructure/SmartMarketBot.Infrastructure.csproj src/SmartMarketBot.Infrastructure/
COPY src/SmartMarketBot.API/SmartMarketBot.API.csproj src/SmartMarketBot.API/

RUN dotnet restore src/SmartMarketBot.API/SmartMarketBot.API.csproj

COPY src/ src/
RUN dotnet publish src/SmartMarketBot.API/SmartMarketBot.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartMarketBot.API.dll"]
