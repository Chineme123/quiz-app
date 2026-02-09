FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/UserService/UserService.API/UserService.API.csproj", "src/Services/UserService/UserService.API/"]
COPY ["src/Services/UserService/UserService.Application/UserService.Application.csproj", "src/Services/UserService/UserService.Application/"]
COPY ["src/Services/UserService/UserService.Domain/UserService.Domain.csproj", "src/Services/UserService/UserService.Domain/"]
COPY ["src/Services/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "src/Services/UserService/UserService.Infrastructure/"]
RUN dotnet restore "./src/Services/UserService/UserService.API/UserService.API.csproj"
COPY . .
WORKDIR "/src/src/Services/UserService/UserService.API"
RUN dotnet build "./UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]
