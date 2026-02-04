#!/bin/bash
set -e

# Project list
SERVICES=("Auth" "User" "Quiz" "Result" "Notification")

# Initialize Solution
echo "Creating Solution..."
dotnet new sln -n QuizApp

# Create Services
for SERVICE in "${SERVICES[@]}"; do
    echo "Scaffolding $SERVICE Service..."
    
    # Define paths
    BASE_PATH="src/Services/${SERVICE}Service"
    API_PROJECT="${SERVICE}Service.API"
    APP_PROJECT="${SERVICE}Service.Application"
    DOMAIN_PROJECT="${SERVICE}Service.Domain"
    INFRA_PROJECT="${SERVICE}Service.Infrastructure"

    # Create directories
    mkdir -p "$BASE_PATH"

    # 1. Domain Layer (Class Library)
    dotnet new classlib -n "$DOMAIN_PROJECT" -o "$BASE_PATH/$DOMAIN_PROJECT" -f net8.0
    
    # 2. Application Layer (Class Library)
    dotnet new classlib -n "$APP_PROJECT" -o "$BASE_PATH/$APP_PROJECT" -f net8.0
    
    # 3. Infrastructure Layer (Class Library)
    dotnet new classlib -n "$INFRA_PROJECT" -o "$BASE_PATH/$INFRA_PROJECT" -f net8.0
    
    # 4. API Layer (Web API)
    dotnet new webapi -n "$API_PROJECT" -o "$BASE_PATH/$API_PROJECT" -f net8.0 --use-controllers

    # Add references (Clean Architecture Dependencies)
    # API -> Application, Infrastructure
    dotnet add "$BASE_PATH/$API_PROJECT/$API_PROJECT.csproj" reference "$BASE_PATH/$APP_PROJECT/$APP_PROJECT.csproj"
    dotnet add "$BASE_PATH/$API_PROJECT/$API_PROJECT.csproj" reference "$BASE_PATH/$INFRA_PROJECT/$INFRA_PROJECT.csproj"
    
    # Infrastructure -> Application, Domain
    dotnet add "$BASE_PATH/$INFRA_PROJECT/$INFRA_PROJECT.csproj" reference "$BASE_PATH/$APP_PROJECT/$APP_PROJECT.csproj"
    dotnet add "$BASE_PATH/$INFRA_PROJECT/$INFRA_PROJECT.csproj" reference "$BASE_PATH/$DOMAIN_PROJECT/$DOMAIN_PROJECT.csproj"
    
    # Application -> Domain
    dotnet add "$BASE_PATH/$APP_PROJECT/$APP_PROJECT.csproj" reference "$BASE_PATH/$DOMAIN_PROJECT/$DOMAIN_PROJECT.csproj"

    # Add required packages to API (Serilog, Swagger, etc. can be added here or later)
    # Adding EF Core Design to API for migrations support usually, or Infrastructure
    dotnet add "$BASE_PATH/$API_PROJECT/$API_PROJECT.csproj" package Microsoft.EntityFrameworkCore.Design --version 8.0.2
    
    # Add EF Core to Infrastructure
    dotnet add "$BASE_PATH/$INFRA_PROJECT/$INFRA_PROJECT.csproj" package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.2
    dotnet add "$BASE_PATH/$INFRA_PROJECT/$INFRA_PROJECT.csproj" package Microsoft.EntityFrameworkCore --version 8.0.2

    # Add projects to solution
    dotnet sln add "$BASE_PATH/$DOMAIN_PROJECT/$DOMAIN_PROJECT.csproj" --solution-folder "$SERVICE"
    dotnet sln add "$BASE_PATH/$APP_PROJECT/$APP_PROJECT.csproj" --solution-folder "$SERVICE"
    dotnet sln add "$BASE_PATH/$INFRA_PROJECT/$INFRA_PROJECT.csproj" --solution-folder "$SERVICE"
    dotnet sln add "$BASE_PATH/$API_PROJECT/$API_PROJECT.csproj" --solution-folder "$SERVICE"

    # Clean up Class1.cs
    rm "$BASE_PATH/$DOMAIN_PROJECT/Class1.cs" 2>/dev/null || true
    rm "$BASE_PATH/$APP_PROJECT/Class1.cs" 2>/dev/null || true
    rm "$BASE_PATH/$INFRA_PROJECT/Class1.cs" 2>/dev/null || true

    echo "$SERVICE Service scaffolded successfully."
done

echo "Scaffolding complete!"
