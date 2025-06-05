# Release Retention Library

A .NET class library that implements the Release Retention rule for DevOps Deploy, helping teams manage their releases by determining which ones should be kept based on deployment activity.

## Overview

The Release Retention Library (`Retention.Lib`) is a self-contained .NET class library that implements the Release Retention rule and is thus reusable. It helps teams determine which releases should be kept based on their deployment activity across different environments. Developers have flexibility on how they implement the Release Retention Library into their solutions. However, as per the Task specifications there is no logic to delete "not to be retained" releases.

### The Release Retention Rule

For each project/environment combination, the library keeps `n` releases that have been most recently deployed, where `n` is the number of releases to keep. A release is considered "deployed" if it has one or more deployments, into one or more environments for one or more projects.

## Core Technology Stack

- **.NET 8.0**
- **C# 12**
- **Microsoft.Extensions.Configuration** - For configuration management
- **Microsoft.Extensions.Configuration.Binder** - For configuration binding
- **Microsoft.Extensions.Http** - For HTTP client functionality
- **Microsoft.Extensions.Logging** - For logging
- **Newtonsoft.Json** - For JSON serialization/deserialization

## Core Components

### Retention.Lib

The main class library that can be used in any .NET solution. Key components include:

#### Interfaces

- `IReleaseRetentionService` - Main service interface for release retention logic
- `IDataProvider` - Interface for data providers (local files or API)
- `IDataProviderFactory` - Factory for creating data providers

#### Main Logic

- `ReleaseRetentionService` - Implements the core retention logic
- `DataProviderFactory` - Initialises `IDataProvider` instances, per data implementation
- `JsonFileDataProvider` - Handles loading data from local JSON files
- `DevOpsDeployClient` - Handles fetching data from the DevOps Deploy API

#### Core Models

- `Project` - Represents a project
- `Environment` - Represents an environment
- `Release` - Represents a release
- `Deployment` - Represents a deployment
- `RetentionResult` - Represents the result of retention analysis
- `RetentionOptions` - Configuration options for the library
- `DevOpsDeployOptions` - Configuration for the DevOps Deploy API client
- `DataFileOptions` - Configuration for local data files

### Data Sources

The library supports two ways of providing data:

1. **Local JSON Files**

   - Projects.json
   - Environments.json
   - Releases.json
   - Deployments.json

2. **DevOps Deploy API**
   - Assumes an API similar to Octopus Deploy
   - Keeps the same JSON structure as the sample data files

## Core Logic Flow

Developers can directly provide their data sources, or use the DevOps Deploy API

<details>
<summary>Local Data File Flow</summary>

1. **Configuration**

   - `RetentionOptions` is configured with `UseLocalData = true`
   - `DataFileOptions` specifies paths to JSON files
   - Files are validated to exist before processing

2. **Data Provider Creation**

   - `DataProviderFactory` creates a `JsonFileDataProvider`
   - Provider is configured with file paths from `DataFileOptions`

3. **Data Loading**

   - `JsonFileDataProvider` reads JSON files:
     1. `Projects.json` → List of projects
     2. `Environments.json` → List of environments
     3. `Releases.json` → List of releases
     4. `Deployments.json` → List of deployments
   - Data is deserialized into strongly-typed models

4. **Retention Analysis**

   - `ReleaseRetentionService` applies the Release Retention rule:
     1. `ValidateAndPrepareData` validates and prepares the data, then
     2. `ProcessRetention` groups deployments by project and environment, then
     3. Orders deployments by date (newest first)
     4. Retains the specified number of releases per project/environment
     5. Returns a procured list of releases to retain

5. **Result Processing**
   - Results are ordered by project and environment
   - Each result includes: - Release ID and version - Project and environment names - Last deployment date
   </details>

<details>
<summary>DevOps Deploy API Flow</summary>

1. **Configuration**

   - `RetentionOptions` is configured with `UseLocalData = false`
   - `DevOpsDeployOptions` specifies API connection details
   - HTTP client is configured with API key and timeout

2. **Data Provider Creation**

   - `DataProviderFactory` creates a `DevOpsDeployClient`
   - Client is configured with API settings from `DevOpsDeployOptions`

3. **API Communication**

   - `DevOpsDeployClient` makes HTTP requests to:
     1. `GET /api/{spaceId}/projects` → List of projects
     2. `GET /api/{spaceId}/environments` → List of environments
     3. `GET /api/{spaceId}/releases` → List of releases
     4. `GET /api/{spaceId}/deployments` → List of deployments
   - Responses are deserialized into strongly-typed models
   - API calls are made on-demand as needed

4. **Retention Analysis**

   - `ReleaseRetentionService` applies the Release Retention rule:
     1. `ValidateAndPrepareData` validates and prepares the data, then
     2. `ProcessRetention` groups deployments by project and environment, then
     3. Orders deployments by date (newest first)
     4. Retains the specified number of releases per project/environment
     5. Returns a procured list of releases to retain

5. **Result Processing**
   - Results are ordered by project and environment
   - Each result includes: - Release ID and version - Project and environment names - Last deployment date
   </details>

## Assumptions

1. **API Design**

   - The DevOps Deploy API follows a similar pattern to Octopus Deploy, an API first solution.
   - API uses a space-based model
   - Authentication is done via API key and a custom header
   - Data is served in existing JSON structures, no changes were made to avoid downstream refactoring
   - API endpoints follow RESTful conventions

2. **Retention Logic**

   - Projects will have at least one release in order to produce a results list of releases to retain
   - Releases are deployed to environments
   - A release can have multiple deployments
   - All entities have unique IDs, nulls are not valid
   - Release versions follow semantic versioning (SemVar 2.0.0)
   - Duplicate Deployments and Releases, only the first is valid
   - Deployments and releases with null values are not valid
   - Deployments into non-existing Environments or Projects, are not valid
   - Releases into non-existing Environments or Projects, are not valid
   - Release Versions that do not conform to SemVar are not valid

3. **Configuration**
   - Local data files are in JSON format, no changes were made to avoid downstream refactoring
   - API configuration includes base API URL, API Header, API key, and space ID
   - Keep count is always positive integer (you can't retain zero releases)

## Installation

### Option 1: Project Reference

1. Clone the repository to a safe place.
2. Add the `Retention.Lib` project to your solution, by opening your solution in Visual Studio and right clicking on the main Solution. Choose from menu `Add` then `Existing Project` from the sub menu.
3. Next, add a project reference to `Retention.Lib`, to your existing project. You can do this by righ clicking on your project and choosing from menu `Add` then `Project Reference` from the sub menu. In the Reference Manager, tick Retention.Lib and OK.
4. Now add your data sources to your project, or grab your settings for your API endpoint.
5. Finally configure your `program.cs` to inject the `Rentention.Lib` and its services into your host. See next section `Example Implementation Usage` for a detailed implementation example. Here you will configure `RetentionOptions` to make use of either the `DataFileOptions` or `DevOpsDeployOptions` API setup. Alternatively, you can create an appsetttings.json or secrets to pass in these settings, and you could implement the `GetReleasesToKeepAsync(keepCount)` in any Controller or Service of your project.


### Option 2: Local NuGet Package

1. Build the NuGet package:
```powershell
# Pull down the Retention Release repository
# Navigate to the Retention.Lib project directory
cd src\Retention.Lib

# Create the NuGet package
dotnet pack -c Release
```
This will create a .nupkg file in `bin/Release/`

2. Add the local package source to nuget:
```powershell
# Add the local package source, specify path to the Rentention.Lib release folder
# Make sure to replace "path\to\Retention.Lib\bin\Release" with the actual path where the .nupkg file is located.
dotnet nuget add source "path\to\Retention.Lib\bin\Release" --name local-release-retention
```

3. Install the package into your solution:

Open your project solution in Visual Studio code.
  - a. Right click on your project and choose `Manage Nuget Packages...` from the menu.
  - b. On the top right corner of the Nuget window, click on the `Package Source:` dropdown and choose `local-release-retention` from the list.
  - c. From the 'Browse' tab you should see `Retention.Lib`. Click on install and accept the prompts.
  - d. Now add your data sources to your project, or grab your settings for your API endpoint.
  - e. Finally configure your `program.cs` to inject the `Rentention.Lib` and its services into your host. See the next section titled `Example Implementation Usage` for a detailed implementation example. Here you will configure `RetentionOptions` to make use of either the `DataFileOptions` or `DevOpsDeployOptions` API setup. Alternatively, you can create an appsetttings.json or secrets to pass in these settings, and you could implement the `GetReleasesToKeepAsync(keepCount)` in any Controller or Service of your project.



## Example Implementation Usage

### Basic Implementation

Here's a simple example of how to use the library in a .NET application:

<details>
<summary>Click to expand example implementation</summary>

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Retention.Lib.Interfaces;
using Retention.Lib.Clients;
using Microsoft.Extensions.Configuration;
using Retention.Lib.Models.Configuration;
using Retention.Lib.Services;
using Retention.Lib.Data;

var builder = Host.CreateApplicationBuilder(args);

// Create and bind retention configuration, depending on whether to use local data or DevOps API.
var useLocalData = builder.Configuration.GetValue<bool>("UseLocalData", false);
var keepCount = builder.Configuration.GetValue<int>("KeepCount", 1);
RetentionOptions retentionOptions;

// Example configuration for local data files or use of the DevOps Deploy API
if (useLocalData)
{
    retentionOptions = new RetentionOptions
    {
        UseLocalData = true,
        KeepCount = keepCount,
        DataFiles = new DataFileOptions
        {
            Projects = Path.Combine(AppContext.BaseDirectory, "Data", "Projects.json"),
            Environments = Path.Combine(AppContext.BaseDirectory, "Data", "Environments.json"),
            Releases = Path.Combine(AppContext.BaseDirectory, "Data", "Releases.json"),
            Deployments = Path.Combine(AppContext.BaseDirectory, "Data", "Deployments.json")
        }
    };
}
else
{
    retentionOptions = new RetentionOptions
    {
        UseLocalData = false,
        KeepCount = keepCount,
        DevOpsDeploy = new DevOpsDeployOptions
        {
            ApiBaseUrl = "http://your-devops-api/",
            ApiKeyHeader = "X-DevOpsDeploy-ApiKey",
            ApiKey = "test-api-key",
            SpaceId = "spaces-1",
            TimeoutSeconds = 30
        }
    };
}

// Validate the retention options to ensure all required fields are set correctly.
retentionOptions.Validate();

// add services to the container
builder.Services.AddSingleton(retentionOptions);
builder.Services.AddHttpClient<IDataProvider, DevOpsDeployClient>();
builder.Services.AddSingleton<IDataProviderFactory, DataProviderFactory>();
builder.Services.AddSingleton<IReleaseRetentionService, ReleaseRetentionService>();

var host = builder.Build();

// Ensure the host is ready and services are available
using var scope = host.Services.CreateScope();
var retentionService = scope.ServiceProvider.GetRequiredService<IReleaseRetentionService>();

// Run the ReleaseRetentionService to determine which releases to keep
var results = await retentionService.GetReleasesToKeepAsync(keepCount);

// Example output of the results
Console.WriteLine($"Keep count specified: {keepCount}");
Console.WriteLine($"Number of releases determined to keep: {results.Count}");

foreach (var result in results)
{
    Console.WriteLine(result);
}
```

Required data files (JSON format):

```json
// Projects.json
[
  {
    "Id": "Project-1",
    "Name": "Project 1"
  }
]

// Environments.json
[
  {
    "Id": "Environment-1",
    "Name": "Environment 1"
  }
]

// Releases.json
[
  {
    "Id": "Release-1",
    "ProjectId": "Project-1",
    "Version": "1.0.0",
    "Created": "2000-01-01T08:00:00Z"
  }
]

// Deployments.json
[
  {
    "Id": "Deploy-1",
    "ReleaseId": "Release-1",
    "EnvironmentId": "Environment-1",
    "DeployedAt": "2000-01-01T10:00:00Z"
  }
]
```

</details>

## Retention.Worker

The `Retention.Worker` is a demonstration application that shows how to use the `Retention.Lib`. It's not part of the core solution but serves as an example implementation.

### Features

- Configurable through `appsettings.json`
- Supports both local data files and API data sources
- Configurable keep count
- Logging of retention analysis results

### Configuration

```json
{
  "Retention": {
    "UseLocalData": true,
    "KeepCount": 2,
    "DataFiles": {
      "Projects": "Data/Projects.json",
      "Environments": "Data/Environments.json",
      "Releases": "Data/Releases.json",
      "Deployments": "Data/Deployments.json"
    },
    "DevOpsDeploy": {
      "ApiBaseUrl": "http://your-devops-api/",
      "ApiKeyHeader": "X-DevOpsDeploy-ApiKey",
      "ApiKey": "your-api-key",
      "SpaceId": "spaces-1"
    }
  }
}
```

### Running the Worker

Either run the `Rentention.Worker` from Visual Studio debugger. Or use Terminal which overrides some configuration options in `appsettings.json`:
```powershell
# Using local data files
dotnet run --project src/Retention.Worker --UseLocalData true --keepcount 5

# Using API data source
dotnet run --project src/Retention.Worker --UseLocalData false --keepcount 5
```

## Testing

The solution includes basic test coverage in the `Retention.Tests` project. Key test areas include:

### Core Logic Tests

- Single release scenarios
- Multiple releases in same environment
- Multiple releases across different environments
- Multiple projects and environments
- Different keep counts
- Empty data sets
- Invalid keep counts
- Releases with invalid dates
- Duplicate deployments
- Json File Data tests
- DevOps Deploy API tests
- And many other tests

### Worker Tests

- Local data loading
- API data loading
- Different keep counts
- Error handling

### Running Tests

Note: integration tests require configuring the `IntegrationTestAPI` endpoint in the `appsettings.json` to execute.

To run the tests from Visual Studio, go to top menu `Test` and choose `Run All Tests`. 

Or in Terminal run:

```powershell
dotnet test
```

## Use of AI Assistance

This project was developed with the assistance of AI tools, primarily using Claude 4.0 Sonnet via Claude Desktop App, And Github Copilot in Visual Studio IDE (GPT 4.1). The AI assistance was used in several key areas:

### Architecture and Design

- Theory crafting, Initial planning, and architecture design based on the Tasks specification requirements
- Design theorizing and decision-making for the core components
- Guidance on dependency injection and service design patterns

### Code Development

- Assistance with improving the core retention release logic
- General Code review and suggestions for improvements
- Help with debugging issues and errors

### Testing

- Help with several test cases, using AAA and Moq
- Assistance in discovering edge cases to test for

The AI was used as a collaborative tool, with all code and architectural decisions being reviewed and debug-step-through by myself. AI is useful to a point, however it struggles with complex logic flow and understanding full project context. In my experience AI is a powerful tool, but all generated code needs to be reviewed and refactored. A good example is using AI to help write Tests, they can be brittle and sometimes not even test the actual unit/functionality or integration being tested.

