# syntax=docker/dockerfile:1

# ---- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartX.sln ./
COPY NuGet.Config ./
COPY src/SmartX.Shared/SmartX.Shared.csproj src/SmartX.Shared/
COPY src/SmartX.Client/SmartX.Client.csproj src/SmartX.Client/
RUN dotnet restore src/SmartX.Client/SmartX.Client.csproj

COPY src/SmartX.Shared/ src/SmartX.Shared/
COPY src/SmartX.Client/ src/SmartX.Client/

RUN dotnet publish src/SmartX.Client/SmartX.Client.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Overridden by docker-compose to point at the api service by name.
ENV ApiBaseUrl=http://smartx-api:8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartX.Client.dll"]
