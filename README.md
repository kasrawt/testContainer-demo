# API + Integration Tests (Testcontainers + Respawn)

Production-parity integration tests for an ASP.NET Core Minimal API using **EF Core + SQL Server**.  
Instead of an in-memory DB, tests spin up a **real SQL Server** in Docker via **Testcontainers**, and reset data between tests with **Respawn** (fast truncation + identity reseed).

## Tech stack

- .NET 8, ASP.NET Core Minimal API  
- EF Core (SqlServer)  
- xUnit + Shouldly  
- Testcontainers for .NET (SQL Server)  
- Respawn (schema-safe reset, identity reseed)

---

## Prerequisites

- **.NET SDK 8.0+**
- **Docker** running (Docker Desktop on Windows/macOS; Docker engine on Linux)
- Visual Studio / Rider / VS Code (optional)

> Windows users: enable WSL2 backend for Docker Desktop.
