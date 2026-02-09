FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/ResultService/ResultService.API/ResultService.API.csproj", "src/Services/ResultService/ResultService.API/"]
COPY ["src/Services/ResultService/ResultService.Application/ResultService.Application.csproj", "src/Services/ResultService/ResultService.Application/"]
COPY ["src/Services/ResultService/ResultService.Domain/ResultService.Domain.csproj", "src/Services/ResultService/ResultService.Domain/"]
COPY ["src/Services/ResultService/ResultService.Infrastructure/ResultService.Infrastructure.csproj", "src/Services/ResultService/ResultService.Infrastructure/"]
RUN dotnet restore "./src/Services/ResultService/ResultService.API/ResultService.API.csproj"
COPY . .
WORKDIR "/src/src/Services/ResultService/ResultService.API"
RUN dotnet build "./ResultService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ResultService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResultService.API.dll"]
