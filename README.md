# Report Visualizer

A comprehensive report viewer application built with .NET technologies (C# and ASP.NET).

## Features

- Database connection management with support for multiple database types
- Template-based report generation
- Report preview and rendering
- Responsive UI design optimized for 1920x1080 resolution
- Centralized error handling and logging
- Modular architecture for easy maintenance and scaling

## Architecture

The application follows a modular architecture with the following components:

### 1. DataAccessLayer
- Handles database connections and data retrieval
- Provides UI for database and table selection
- Manages database configuration models

### 2. ReportTemplates
- Stores predefined report templates (.rdlc files)
- Provides template editing capabilities
- Validates template structure and content

### 3. ReportDevelopment
- Handles template selection for report creation
- Manages report generation logic
- Provides report preview functionality

### 4. ReportViewer
- Provides UI for report selection
- Handles final report rendering and display

### 5. Utilities
- Manages application configuration and environment variables
- Maintains a single database connection throughout the application lifecycle
- Handles application logging and error management

### 6. App_Start
- Initializes database connections on application startup

## Setup Instructions

### Prerequisites
- .NET 6.0 SDK or later
- SQL Server (or compatible database)
- Visual Studio 2022 (recommended)

### Installation Steps

1. Clone the repository
   ```
   git clone https://github.com/yourusername/ReportVisualizer.git
   ```

2. Open the solution in Visual Studio
   ```
   cd ReportVisualizer
   start ReportVisualizer.sln
   ```

3. Restore NuGet packages
   ```
   dotnet restore
   ```

4. Configure database connection
   - Update the connection string in `appsettings.json`
   - Or set environment variables for database credentials

5. Build and run the application
   ```
   dotnet build
   dotnet run
   ```

## Configuration

### Database Connection
Database credentials are stored in environment variables using the ConfigurationManager. Set the following environment variables:

- `DB_SERVER` - Database server address
- `DB_NAME` - Default database name
- `DB_USER` - Database username (if not using integrated security)
- `DB_PASSWORD` - Database password (if not using integrated security)
- `DB_INTEGRATED_SECURITY` - Set to "true" to use Windows authentication

### Report Templates
Place your .rdlc template files in the `ReportTemplates/PredefinedTemplates` directory.

## Usage

1. Configure database connection in the Database Config section
2. Select or create a report template
3. Generate a report by selecting data sources and parameters
4. View and export the generated report

## Error Handling

The application implements comprehensive error handling and validation:
- All user inputs are validated before processing
- Template creation includes validation for proper structure
- Errors are logged centrally and displayed to users when appropriate
- Database connection issues are handled gracefully

## Responsive Design

The UI is designed to be responsive with optimal display at 1920x1080 resolution.