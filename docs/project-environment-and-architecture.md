# Project Environment & Architecture

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

### 🛠 Project Environment & Architecture

Core Framework: .NET 10.0.

Infrastructure: Docker Compose with SQL Server 2022 and 5 Microservices (Auth, User, Quiz, Result, Notification).

Development Host: GitHub Codespaces (Linux) accessed via Remote-SSH or GitHub CLI.

### 🛑 Encountered Problems & Solutions

#### 1. Database Connectivity (The "Error 40" / "Error 35" Issue)

Problem: The API could not find the SQL Server, leading to transient failures or "Network-related" errors.

Cause: SQL Server takes longer to boot than the .NET app, and Docker networking requires specific service names rather than localhost.

Solution: * Enabled EnableRetryOnFailure() in Program.cs to handle the boot-up lag.

Used Server=sqlserver in the connection string for internal Docker communication.

Used Server=localhost in appsettings.Development.json only when running migrations from the host terminal.

#### 2. EF Core Change Tracking (The ValueComparer Warning)

Problem: MultipleChoiceQuestion.Options (a string collection) triggered a warning about missing a value comparer.

Solution: Implemented a ValueComparer<List<string>> in the OnModelCreating method of QuizDbContext.cs to allow EF Core to track individual element changes.

Missing Namespace: Resolved CS0246 by adding using Microsoft.EntityFrameworkCore.ChangeTracking;.

#### 3. Remote Authentication (403 Forbidden)

Problem: GitHub CLI (gh) on a new machine (HP Laptop) could see codespaces but couldn't connect.

Solution: Refreshed the authentication token with the specific codespace scope using gh auth refresh -s codespace.

### 📜 Common Terminal Scripts

#### Docker Management

Bash

# Start all services in the background

docker compose up -d

# Check the status of your microservices

docker compose ps

# View real-time logs for a specific service (e.g., QuizService)

docker compose logs quizservice -f

#### Entity Framework (EF) Core

Bash

# List migrations (Run from the API project directory)

dotnet ef migrations list --project src/Services/QuizService/QuizService.API/

# Apply migrations to the database

dotnet ef database update --project src/Services/QuizService/QuizService.API/

#### GitHub CLI (Remote Access)

Bash

# Connect to your codespace from a new machine

gh codespace ssh

# Open the codespace directly in Antigravity/VS Code

gh codespace code -r Chineme123/quiz-app

### 🏗 Setup Blueprint for New Projects

Containerize Early: Always use a docker-compose.yml to define your SQL Server and dependencies to avoid local installation "Error 40" issues.

Infrastructure Resiliency: Always wrap your UseSqlServer call with sqlOptions.EnableRetryOnFailure().

Port Forwarding: Use the Ports tab in your IDE to map internal container ports (like 8080) to your local machine (like 5224).

Swagger Access: Always check for app.UseSwagger() in Program.cs and navigate to /swagger on the forwarded port to test your endpoints.

#### Final Git Push Note

Before you close a session, always run:

git add .

git commit -m "Your descriptive message"

git push origin main
