#!/bin/bash
set -e

SERVICES=("Auth" "User" "Quiz" "Result" "Notification")

for SERVICE in "${SERVICES[@]}"; do
    echo "Adding Dockerfile to $SERVICE Service..."
    
    API_DIR="src/Services/${SERVICE}Service/${SERVICE}Service.API"
    PROJECT_NAME="${SERVICE}Service.API"

    cat <<EOF > "$API_DIR/Dockerfile"
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/${SERVICE}Service/${SERVICE}Service.API/${SERVICE}Service.API.csproj", "src/Services/${SERVICE}Service/${SERVICE}Service.API/"]
COPY ["src/Services/${SERVICE}Service/${SERVICE}Service.Application/${SERVICE}Service.Application.csproj", "src/Services/${SERVICE}Service/${SERVICE}Service.Application/"]
COPY ["src/Services/${SERVICE}Service/${SERVICE}Service.Domain/${SERVICE}Service.Domain.csproj", "src/Services/${SERVICE}Service/${SERVICE}Service.Domain/"]
COPY ["src/Services/${SERVICE}Service/${SERVICE}Service.Infrastructure/${SERVICE}Service.Infrastructure.csproj", "src/Services/${SERVICE}Service/${SERVICE}Service.Infrastructure/"]
RUN dotnet restore "./src/Services/${SERVICE}Service/${SERVICE}Service.API/${SERVICE}Service.API.csproj"
COPY . .
WORKDIR "/src/src/Services/${SERVICE}Service/${SERVICE}Service.API"
RUN dotnet build "./${SERVICE}Service.API.csproj" -c \$BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./${SERVICE}Service.API.csproj" -c \$BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "${SERVICE}Service.API.dll"]
EOF

    echo "Dockerfile created for $SERVICE"
done
