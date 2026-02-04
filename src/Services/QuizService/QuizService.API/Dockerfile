FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/QuizService/QuizService.API/QuizService.API.csproj", "src/Services/QuizService/QuizService.API/"]
COPY ["src/Services/QuizService/QuizService.Application/QuizService.Application.csproj", "src/Services/QuizService/QuizService.Application/"]
COPY ["src/Services/QuizService/QuizService.Domain/QuizService.Domain.csproj", "src/Services/QuizService/QuizService.Domain/"]
COPY ["src/Services/QuizService/QuizService.Infrastructure/QuizService.Infrastructure.csproj", "src/Services/QuizService/QuizService.Infrastructure/"]
RUN dotnet restore "./src/Services/QuizService/QuizService.API/QuizService.API.csproj"
COPY . .
WORKDIR "/src/src/Services/QuizService/QuizService.API"
RUN dotnet build "./QuizService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./QuizService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuizService.API.dll"]
