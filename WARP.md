# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Splity is a serverless expense sharing application built with .NET 8 and AWS Lambda that automates receipt processing through Azure Document Intelligence. It uses a microservices architecture with separate Lambda functions for different operations and PostgreSQL for data persistence.

## Architecture

### Core Components

- **Lambda Functions**: Independent microservices for each operation (Party management, Expense management)
- **Shared Libraries**: Common functionality across all Lambda functions
  - `Splity.Shared.Database`: Data models, repositories, and PostgreSQL connection handling
  - `Splity.Shared.AI`: Azure Document Intelligence service integration  
  - `Splity.Shared.Storage`: S3 bucket operations for receipt image storage
  - `Splity.Shared.Common`: API Gateway helpers and common utilities

### Lambda Function Structure

Each Lambda function follows a consistent pattern:
- `src/Splity.{Domain}.{Action}/`: Main Lambda function code
- `test/Splity.{Domain}.{Action}.Tests/`: XUnit test project
- Functions use dependency injection with repository pattern
- API Gateway HTTP v2 proxy integration with CORS support

### Data Flow

1. Client uploads receipt through API Gateway
2. Lambda function stores image in S3 bucket
3. Azure Document Intelligence extracts receipt data
4. Processed data stored in PostgreSQL database
5. Party and expense management through dedicated Lambda functions

## Development Commands

### Building and Testing

```bash
# Restore dependencies for entire solution
dotnet restore

# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run tests for specific project
dotnet test Splity.Party.Create/test/Splity.Party.Create.Tests/

# Build specific Lambda function
dotnet build Splity.Party.Create/src/Splity.Party.Create/
```

### Working with Individual Lambda Functions

```bash
# Build a specific Lambda function for deployment
dotnet publish Splity.Party.Create/src/Splity.Party.Create/ -c Release -o ./publish

# Run tests for a specific function
dotnet test Splity.Expenses.Extract/test/Splity.Expenses.Extract.Tests/
```

### Package Management

```bash
# Add package to shared library
dotnet add Splity.Shared.Database/Splity.Shared.Database.csproj package [PackageName]

# Add package to specific Lambda function
dotnet add Splity.Party.Create/src/Splity.Party.Create/Splity.Party.Create.csproj package [PackageName]
```

## Environment Configuration

### Required Environment Variables

```bash
# AWS Configuration
export AWS_BUCKET_NAME="split-app-v1"
export AWS_BUCKET_REGION="eu-central-1"

# Database Configuration
export CLUSTER_USERNAME="admin" 
export CLUSTER_HOSTNAME="[your-cluster-hostname]"
export CLUSTER_DATABASE="postgres"

# Azure Document Intelligence
export DOCUMENT_INTELLIGENCE_ENDPOINT="https://di-document-reader-test.cognitiveservices.azure.com/"
export DOCUMENT_INTELLIGENCE_API_KEY="[your-api-key]"

# CORS Configuration
export ALLOWED_ORIGINS="*"
```

## Project Structure Guidelines

### Lambda Functions
- **Party Management**: `Splity.Party.{Create|Delete|Get}`
- **Expense Management**: `Splity.Expenses.{Create|Delete|Get|Extract}`
- All functions use AWS Lambda runtime with .NET 8
- API Gateway integration with HTTP v2 proxy events

### Shared Libraries
- Database models in `Splity.Shared.Database/Models/`
- Repository interfaces in `Splity.Shared.Database/Repositories/Interfaces/`
- Repository implementations in `Splity.Shared.Database/Repositories/`

### Testing Strategy
- Each Lambda function has corresponding test project
- Tests use XUnit framework with Amazon.Lambda.TestUtilities
- Repository pattern enables easy mocking for unit tests

## Dependencies

### Key NuGet Packages
- **AWS**: Amazon.Lambda.Core, Amazon.Lambda.APIGatewayEvents, AWSSDK.Core
- **Database**: Npgsql for PostgreSQL connectivity, AWSSDK.DSQL
- **AI**: Azure.AI.DocumentIntelligence
- **Testing**: xunit, Amazon.Lambda.TestUtilities

### External Services
- **AWS Lambda**: Serverless compute platform
- **Amazon S3**: Receipt image storage  
- **Amazon API Gateway**: REST API endpoint management
- **PostgreSQL**: Primary database (AWS-managed)
- **Azure Document Intelligence**: Receipt OCR and data extraction

## Common Development Tasks

### Adding New Lambda Function
1. Create function directory following `Splity.{Domain}.{Action}` pattern
2. Add `src/` and `test/` subdirectories
3. Reference required shared libraries in `.csproj`
4. Implement Function class with `FunctionHandler` method
5. Add function to `Splity.sln`

### Working with Database
- Connection management through `DsqlConnectionHelper`  
- Repository pattern for data access
- Models defined in `Splity.Shared.Database/Models/`

### Receipt Processing Integration
- Upload images via `S3BucketService`
- Process with `DocumentIntelligenceService` 
- Extract data gets stored via repositories

## Deployment Infrastructure

### AWS Resources
- Lambda Functions: All `Splity*` functions
- S3 Bucket: `split-app-v1` (eu-central-1)
- PostgreSQL Database: AWS-managed cluster
- API Gateway: HTTP API with Lambda proxy integration

Infrastructure diagram available at `docs/infra.dot`.

## Create new Lambda Function project steps

### Prerequisites
- Ensure you have the `Amazon.Lambda.Templates` NuGet package installed: `dotnet new install Amazon.Lambda.Templates`
- Have AWS Lambda Tools installed: `dotnet tool install -g Amazon.Lambda.Tools`

### Steps

1. **Create the Lambda function project**:
   ```bash
   # Replace {Entity} and {Action} with your specific values (e.g., Party, Update)
   dotnet new lambda.EmptyFunction -n Splity.{Entity}.{Action} -o "$(git rev-parse --show-toplevel)/Splity.{Entity}.{Action}"
   ```

2. **Create the test project**:
   ```bash
   # Navigate to the function directory
   cd "Splity.{Entity}.{Action}"
   ```

3. **Add projects to solution**:
   ```bash
   # From repository root
   dotnet sln Splity.sln add Splity.{Entity}.{Action}/src/Splity.{Entity}.{Action}/Splity.{Entity}.{Action}.csproj
   dotnet sln Splity.sln add Splity.{Entity}.{Action}/test/Splity.{Entity}.{Action}.Tests/Splity.{Entity}.{Action}.Tests.csproj
   ```

4. **Add projects to respective solution folder**:
   ```bash
    dotnet sln Splity.sln add --solution-folder src Splity.{Entity}.{Action}/src/Splity.{Entity}.{Action}/Splity.{Entity}.{Action}.csproj
    dotnet sln Splity.sln add --solution-folder tests Splity.{Entity}.{Action}/test/Splity.{Entity}.{Action}.Tests/Splity.{Entity}.{Action}.Tests.csproj
   ```

5. **Add test references** (in the test project):
   ```bash
   cd ../../test/Splity.{Entity}.{Action}.Tests
   dotnet add reference ../../src/Splity.{Entity}.{Action}/Splity.{Entity}.{Action}.csproj
   dotnet add package Amazon.Lambda.TestUtilities
   dotnet add package FluentAssertions
   ```

6. **Update aws-lambda-tools-defaults.json**:
   ```json
   {
     "Information": [
       "All the command line options for the Lambda command can be specified in this file."
     ],
     "profile": "",
     "region": "eu-west-2",
     "configuration": "Release",
     "function-architecture": "x86_64",
     "function-runtime": "dotnet8",
     "function-memory-size": 128,
     "function-timeout": 15,
     "function-handler": "Splity.{Entity}.{Action}::Splity.{Entity}.{Action}.Function::FunctionHandler",
     "function-name": "Splity{Action}{Entity}",
     "environment-variables": {
       "CLUSTER_HOSTNAME": "",
       "CLUSTER_USERNAME": "admin",
       "CLUSTER_DATABASE": "postgres",
       "AWS_BUCKET_NAME": "split-app-v1",
       "AWS_BUCKET_REGION": "eu-central-1",
       "ALLOWED_ORIGINS": "*"
     }
   }
   ```

7. **Build and test**:
   ```bash
   # From repository root
   dotnet build
   dotnet test Splity.{Entity}.{Action}/test/Splity.{Entity}.{Action}.Tests/
   ```

## Deploy service to AWS

### Prerequisites
- AWS CLI configured with appropriate credentials
- AWS Lambda Tools installed: `dotnet tool install -g Amazon.Lambda.Tools`
- Ensure your function builds successfully: `dotnet build`

### Deployment Steps

1. **Navigate to the Lambda function directory**:
   ```bash
   cd Splity.{Entity}.{Action}/src/Splity.{Entity}.{Action}
   ```

2. **Deploy the function**:
   ```bash
   dotnet lambda deploy-function
   ```

3. **Verify deployment**:
   ```bash
   # List your Lambda functions to confirm deployment
   aws lambda list-functions --query 'Functions[?starts_with(FunctionName, `Splity`)].FunctionName'
   ```

### Deployment Notes
- The deployment uses settings from `aws-lambda-tools-defaults.json`
- If the function doesn't exist, it will be created
- If it exists, it will be updated with new code
- Environment variables are set automatically from the defaults file
- Function timeout is set to 30 seconds (adjustable based on requirements)
