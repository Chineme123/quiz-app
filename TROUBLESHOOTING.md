# 📘 QuizApp Technical Documentation & Troubleshooting Log

This documentation serves as a technical reference for the **QuizApp** microservices project. It captures the specific hurdles encountered during the .NET 10 migration within a GitHub Codespace environment and provides the solutions and scripts needed to resolve them.

---

## 1. Project Architecture & Environment
* **Framework:** .NET 10.0.
* **Architecture:** Microservices (Auth, User, Quiz, Result, Notification).
* **Infrastructure:** Docker Compose with SQL Server 2022.
* **Development Host:** GitHub Codespaces (Linux) accessed via Remote-SSH or GitHub CLI.

---

## 2. Troubleshooting Log: Problems & Solutions

### A. Database Connectivity ("Error 40" / "Error 35")
* **Problem:** The API failed to connect to SQL Server, throwing "Network-related or instance-specific error" or "Name or service not known".
* **Root Cause:** 1. **Race Condition:** SQL Server takes longer to boot than the .NET application, causing the initial connection attempt to fail.
    2. **Networking Context:** The terminal (running `dotnet ef`) runs on the "Host" VM, while the API runs inside a "Container." The Host needs `localhost`, but containers need the service name `sqlserver`.
* **Solution:**
    1. **Resiliency:** Implemented `EnableRetryOnFailure()` in `Program.cs` to handle the startup delay automatically.
    2. **Dual Connection Strings:** * Used `Server=sqlserver` in `docker-compose.yml` for inter-container communication.
        * Used `Server=localhost` in `appsettings.Development.json` for running migrations from the terminal.

### B. EF Core Change Tracking (`ValueComparer` Warning)
* **Problem:** The `MultipleChoiceQuestion.Options` property (a `List<string>`) triggered a warning: *"The property is a collection... with no value comparer"*.
* **Root Cause:** EF Core cannot automatically detect changes inside a complex list of strings without a specific comparer.
* **Solution:**
    1. Added `using Microsoft.EntityFrameworkCore.ChangeTracking;` to the header of `QuizDbContext.cs`.
    2. Implemented a `ValueComparer<List<string>>` in the `OnModelCreating` method to properly track element additions/removals.

### C. Remote Access & Authentication (403 Forbidden)
* **Problem:** The GitHub CLI (`gh`) on a new Windows laptop listed the Codespace but failed to connect with a 403 error.
* **Root Cause:** The authentication token was stale or lacked the specific `codespace` permission scope required to open a remote session.
* **Solution:** * Ran `gh auth refresh -s codespace` to update permissions.
    * Used `gh codespace code` to bypass the VS Code UI and force a direct connection.

---

## 3. Essential Terminal Scripts (Cheat Sheet)

### 🐳 Docker Management
Use these to manage your microservices without leaving the terminal.
```bash
# Start all services in the background (detached mode)
docker compose up -d

# Check the status of your microservices (Up/Exited)
docker compose ps

# View real-time logs for a specific service (e.g., QuizService) to debug crashes
docker compose logs quizservice -f 

# Stop and remove all containers
docker compose down

# Create a new migration (replace 'Name' with your migration name)
dotnet ef migrations add InitialCreate --project src/Services/QuizService/QuizService.API/

# Apply pending migrations to the database
dotnet ef database update --project src/Services/QuizService/QuizService.API/

# List applied migrations to verify database state
dotnet ef migrations list --project src/Services/QuizService/QuizService.API/

# List all available codespaces
gh codespace list

# Connect to the codespace via SSH (Terminal only)
gh codespace ssh

# Open the codespace directly in Antigravity/VS Code
gh codespace code -r Chineme123/quiz-app

Containerize First: Define docker-compose.yml early to avoid local environment mismatches. Ensure you map ports explicitly (e.g., 5224:8080).

Environment Variables: Verify that ASPNETCORE_ENVIRONMENT=Development is set in your Docker Compose file so Swagger UI loads correctly.

Port Forwarding: Always check the Ports tab in your IDE. If a service shows as "Private," right-click and set to "Public" if you need external access.

Swagger URL: The correct URL pattern is http://localhost:[ExternalPort]/swagger (e.g., http://localhost:5224/swagger).
