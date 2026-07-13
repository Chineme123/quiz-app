# The gateway image is the whole front door: it serves the SPA and forwards /api.
# Build context is the repo root (see docker-compose.yml / deploy).

# 1. Build the SPA. design-system/ is copied because the Tailwind entry imports the
#    design tokens from it (../../../design-system).
FROM node:20 AS spa
WORKDIR /spa
COPY frontend/package.json frontend/package-lock.json ./frontend/
RUN cd frontend && npm ci
COPY frontend/ ./frontend/
COPY design-system/ ./design-system/
RUN cd frontend && npm run build

# 2. Build + publish the gateway.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gateway/Gateway.csproj", "src/Gateway/"]
RUN dotnet restore "src/Gateway/Gateway.csproj"
COPY src/Gateway/ ./src/Gateway/
RUN dotnet publish "src/Gateway/Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 3. Runtime: gateway + the SPA baked into wwwroot.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
COPY --from=spa /spa/frontend/dist ./wwwroot
ENTRYPOINT ["dotnet", "Gateway.dll"]
