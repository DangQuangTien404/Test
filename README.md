# Data Labeling Support System API

## Overview
This is the backend API for the Data Labeling Support System. It provides services for project management, data labeling tasks, user management, and quality review. The system is built using .NET 8 and Entity Framework Core with SQL Server.

## Architecture
The solution follows a layered architecture:
- **API**: The presentation layer (Controllers, Middleware).
- **BLL (Business Logic Layer)**: Services and business rules.
- **DAL (DataAccess Layer)**: Repositories and Database Context.
- **DTOs**: Data Transfer Objects and Entities.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (or LocalDB)
- `dotnet-ef` tool (for database migrations)

## Setup & Configuration

### 1. Clone the Repository
```bash
git clone <repository-url>
cd DataLabelingSupportSystem
```

### 2. Configuration
Update `API/appsettings.json` with your database connection string and JWT settings.

**Default `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DataLabelingDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "YOUR_SECURE_KEY_HERE...",
    "Issuer": "DataLabelingSystem",
    "Audience": "DataLabelingUsers"
  }
}
```
*Note: Ensure the JWT Key is at least 32 characters long.*

### 3. Database Setup
The project uses Entity Framework Core Code First. You need to apply migrations to create the database.

First, install the EF Core tools if you haven't already:
```bash
dotnet tool install --global dotnet-ef
```

Then, run the migrations from the root directory:
```bash
# Create the initial migration
dotnet ef migrations add InitialCreate --project DAL --startup-project API

# Apply the migration to the database
dotnet ef database update --project DAL --startup-project API
```

## Running the Application

To run the API:
```bash
cd API
dotnet run
```

The application will start, and data seeding (initial users and projects) will run automatically if the database is empty.

## API Documentation
Once the application is running, you can access the Swagger UI to explore and test the endpoints:

**URL**: `http://localhost:5000/swagger` (or the port shown in your console)

## Frontend Integration
The API is configured to allow CORS requests from `http://localhost:3000` (default React port).
- **Policy Name**: `AllowReactApp`
- **Headers**: Allowed all
- **Methods**: Allowed all
- **Credentials**: Allowed

To change the allowed origin, update the `WithOrigins` value in `API/Program.cs`.

## Default Users (Seeded Data)
After the first run, the database will be populated with:
- **Manager**: `Manager@gmail.com` / `123456`
- **Annotators**: `Staff1@gmail.com` ... `Staff5@gmail.com` / `123456`
