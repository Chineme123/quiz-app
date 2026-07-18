# The Quiztin host image is the whole app: it serves the SPA and owns /api (spec 0007).
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

# 2. Build + publish the host and its module projects.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Quiztin.Api/Quiztin.Api.csproj", "src/Quiztin.Api/"]
COPY ["src/Quiztin.Modules.Identity/Quiztin.Modules.Identity.csproj", "src/Quiztin.Modules.Identity/"]
COPY ["src/Quiztin.Modules.Assessment/Quiztin.Modules.Assessment.csproj", "src/Quiztin.Modules.Assessment/"]
RUN dotnet restore "src/Quiztin.Api/Quiztin.Api.csproj"
COPY src/Quiztin.Api/ ./src/Quiztin.Api/
COPY src/Quiztin.Modules.Identity/ ./src/Quiztin.Modules.Identity/
COPY src/Quiztin.Modules.Assessment/ ./src/Quiztin.Modules.Assessment/
RUN dotnet publish "src/Quiztin.Api/Quiztin.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 3. Runtime: the host + the SPA baked into wwwroot.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
COPY --from=spa /spa/frontend/dist ./wwwroot
ENTRYPOINT ["dotnet", "Quiztin.Api.dll"]
