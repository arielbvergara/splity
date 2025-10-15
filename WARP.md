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

## Configuration Management

### Parameter Store Integration

Splity uses AWS Systems Manager Parameter Store for centralized configuration management. This provides several benefits:

- **Centralized Management**: All configuration in one place
- **Environment Separation**: Different configs for dev, staging, prod
- **Secure Storage**: Sensitive data encrypted using AWS KMS
- **Audit Trail**: Track configuration changes
- **Fallback Support**: Automatic fallback to environment variables

#### Parameter Store Setup

```bash
# Run the automated setup script
./scripts/setup-parameter-store.sh [environment] [region]

# Example for dev environment
./scripts/setup-parameter-store.sh dev eu-west-2

# Example for production environment  
./scripts/setup-parameter-store.sh prod eu-west-2
```

#### Parameter Structure

Parameters are organized hierarchically by environment:

```
/splity/{environment}/
├── database/
│   ├── username          # Database username
│   ├── hostname          # Database hostname (set by CloudFormation)
│   ├── name              # Database name  
│   └── region            # Database region
├── aws/
│   ├── bucket/
│   │   ├── name          # S3 bucket name
│   │   └── region        # S3 bucket region
│   └── region            # AWS region
├── azure/
│   └── document-intelligence/
│       ├── endpoint      # Azure Document Intelligence endpoint
│       └── api-key       # Azure API key (SecureString)
├── cognito/
│   ├── user-pool-id      # Cognito User Pool ID
│   └── client-id         # Cognito Client ID
└── application/
    └── allowed-origins   # CORS allowed origins
```

#### Manual Parameter Management

```bash
# Create or update a parameter
aws ssm put-parameter \
  --name "/splity/dev/aws/bucket/name" \
  --value "split-app-v1" \
  --type "String" \
  --overwrite

# Create a secure parameter (encrypted)
aws ssm put-parameter \
  --name "/splity/dev/azure/document-intelligence/api-key" \
  --value "your-secret-key" \
  --type "SecureString" \
  --overwrite

# List all parameters for an environment
aws ssm get-parameters-by-path \
  --path "/splity/dev/" \
  --recursive

# Get a specific parameter with decryption
aws ssm get-parameter \
  --name "/splity/dev/azure/document-intelligence/api-key" \
  --with-decryption
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
- Parameter Store configured (see Configuration Management section)
- Ensure your function builds successfully: `dotnet build`

### Deployment Steps with Parameter Store

1. **Set up Parameter Store** (one-time per environment):
   ```bash
   ./scripts/setup-parameter-store.sh dev eu-west-2
   ```

2. **Deploy CloudFormation stack** (includes Parameter Store resources):
   ```bash
   aws cloudformation deploy \
     --template-file splity-infrastructure-cf-template.yaml \
     --stack-name splity-complete-infrastructure-dev \
     --parameter-overrides Environment=dev \
     --capabilities CAPABILITY_NAMED_IAM \
     --region eu-west-2
   ```

3. **Deploy All Lambda Code After Infrastructure:**
    After deploying the infrastructure, update all Lambda functions with actual .NET code:
    ```bash
    # Party Functions
    cd Splity.Party.Create/src/Splity.Party.Create && dotnet lambda deploy-function --function-name SplityCreateParty-dev --region eu-west-2
    cd ../../../Splity.Party.Get/src/Splity.Party.Get && dotnet lambda deploy-function --function-name SplityGetParty-dev --region eu-west-2
    cd ../../../Splity.Party.Update/src/Splity.Party.Update && dotnet lambda deploy-function --function-name SplityUpdateParty-dev --region eu-west-2
    cd ../../../Splity.Party.Delete/src/Splity.Party.Delete && dotnet lambda deploy-function --function-name SplityDeleteParty-dev --region eu-west-2

       # Expense Functions
    cd ../../../Splity.Expenses.Create/src/Splity.Expenses.Create && dotnet lambda deploy-function --function-name SplityCreateExpenses-dev --region eu-west-2
    cd ../../../Splity.Expenses.Delete/src/Splity.Expenses.Delete && dotnet lambda deploy-function --function-name SplityDeleteExpenses-dev --region eu-west-2
    cd ../../../Splity.Expenses.Extract/src/Splity.Expenses.Extract && dotnet lambda deploy-function --function-name SplityExtractExpenses-dev --region eu-west-2

       # User Functions
    cd ../../../Splity.User.Create/src/Splity.User.Create && dotnet lambda deploy-function --function-name SplityCreateUser-dev --region eu-west-2
    cd ../../../Splity.User.Get/src/Splity.User.Get && dotnet lambda deploy-function --function-name SplityGetUser-dev --region eu-west-2
    cd ../../../Splity.User.Update/src/Splity.User.Update && dotnet lambda deploy-function --function-name SplityUpdateUser-dev --region eu-west-2
    ```

5. **Verify deployment**:
   ```bash
   # List your Lambda functions to confirm deployment
   aws lambda list-functions --query 'Functions[?starts_with(FunctionName, `Splity`)].FunctionName'
   
   # Test Parameter Store access
   aws ssm get-parameters-by-path --path "/splity/dev/" --recursive
   ```

### Troubleshooting Parameter Store

#### Common Issues

1. **Parameter Store permissions error**:
   ```bash
   # Check if Lambda execution role has SSM permissions
   aws iam get-role-policy --role-name SplityPartyLambdaRole-dev --policy-name DsqlPermissions
   ```

2. **Configuration not loading**:
   - Lambda functions automatically fall back to environment variables
   - Check CloudWatch logs for initialization errors
   - Verify parameters exist: `aws ssm get-parameters-by-path --path "/splity/dev/" --recursive`

3. **SecureString decryption errors**:
   ```bash
   # Test manual decryption
   aws ssm get-parameter --name "/splity/dev/azure/document-intelligence/api-key" --with-decryption
   ```

4. **Performance considerations**:
   - Configuration is cached after first load per Lambda container
   - Cold starts may be slightly slower due to Parameter Store calls
   - Consider using environment variables for frequently accessed, non-sensitive config

### Deployment Notes
- If the function doesn't exist, it will be created
- If it exists, it will be updated with new code
- Environment variables are set automatically from the defaults file
- Function timeout is set to 30 seconds (adjustable based on requirements)
- To delete stack after rollback, run:
```bash
    aws cloudformation delete-stack \ 
    --stack-name splity-complete-infrastructure-dev \
    --region eu-west-2
```
- **Environment Variable Updates**: If you need to update environment variables after deployment (e.g., actual hostnames vs placeholders), use:
  ```bash
    aws lambda update-function-configuration \
    --function-name [FunctionName] \
    --region eu-west-2 \
    --environment Variables="{KEY1=VALUE1,KEY2=VALUE2}"
  ```
  Use key=value format separated by commas, not JSON format. Values should match those in `aws-lambda-tools-defaults.json`.


### Suggestion on how to write a good enough commit message

Use a subset of the types in the specification.
- feat: when making changes that are in line with adding code to support a new feature
- fix: when making code changes to fix previous code
- refactor: for any other code changes that do not change the functionality and behavior of the code
- chore: When changing configuration files or other scripts
- test: When adding tests to the codebase (that were not added alongside the feature itself 😬)

Use the footer to add reference to the Jira issue(s) (if applicable)
Use the title for a short description which can be readable in git clients or gitlab
Describe the changes (if the title is not enough) in an imperative way (i.e.: Calculate review score using XYZ method)

### CloudFormation Templates

#### Complete Infrastructure Template
The entire Splity application infrastructure is defined in `splity-infrastructure-cf-template.yaml`.

**Deploy Complete Infrastructure:**
```bash
# Deploy complete Splity infrastructure stack
aws cloudformation deploy \
  --template-file splity-infrastructure-cf-template.yaml \
  --stack-name splity-complete-infrastructure-dev \
  --parameter-overrides Environment=dev \
  --capabilities CAPABILITY_NAMED_IAM \
  --region eu-west-2

# Deploy with custom parameters
aws cloudformation deploy \
  --template-file splity-infrastructure-cf-template.yaml \
  --stack-name splity-complete-infrastructure-dev \
  --parameter-overrides \
    Environment=dev \
    ClusterHostname=your-cluster-hostname \
    S3BucketName=split-app-v1 \
    DocumentIntelligenceApiKey=your-api-key \
  --capabilities CAPABILITY_NAMED_IAM \
  --region eu-west-2
```