#!/bin/bash

# Script to create a new Lambda function following Splity project structure
# Usage: ./create-lambda-function.sh <Entity> <Action>
# Example: ./create-lambda-function.sh Party Update

set -e

# Check if correct number of arguments provided
if [ $# -ne 2 ]; then
    echo "Usage: $0 <Entity> <Action>"
    echo "Example: $0 Party Update"
    exit 1
fi

ENTITY=$1
ACTION=$2
PROJECT_NAME="Splity.${ENTITY}.${ACTION}"
REPO_ROOT=$(git rev-parse --show-toplevel)

echo "Creating Lambda function: ${PROJECT_NAME}"

# Step 1: Create the Lambda function project
echo "Step 1: Creating Lambda function project..."
dotnet new lambda.EmptyFunction -n "${PROJECT_NAME}" -o "${REPO_ROOT}/${PROJECT_NAME}"

# Step 2: Navigate to function directory (for context)
cd "${REPO_ROOT}/${PROJECT_NAME}"

# Step 3: Add projects to respective solution folders
echo "Step 3: Adding projects to solution folders..."
cd "${REPO_ROOT}"
dotnet sln Splity.sln add --solution-folder src "${PROJECT_NAME}/src/${PROJECT_NAME}/${PROJECT_NAME}.csproj"
dotnet sln Splity.sln add --solution-folder tests "${PROJECT_NAME}/test/${PROJECT_NAME}.Tests/${PROJECT_NAME}.Tests.csproj"

# Step 4: Update Amazon.Lambda.Core to latest version in both projects
echo "Step 4: Updating Amazon.Lambda.Core to latest version..."
cd "${REPO_ROOT}/${PROJECT_NAME}/src/${PROJECT_NAME}"
dotnet add package Amazon.Lambda.Core

cd "${REPO_ROOT}/${PROJECT_NAME}/test/${PROJECT_NAME}.Tests"
dotnet add package Amazon.Lambda.Core

# Step 5: Add test references and packages
echo "Step 5: Adding test project references and packages..."
dotnet add reference "../../src/${PROJECT_NAME}/${PROJECT_NAME}.csproj"
dotnet add package Amazon.Lambda.TestUtilities
dotnet add package FluentAssertions

# Step 6: Update aws-lambda-tools-defaults.json
echo "Step 6: Updating aws-lambda-tools-defaults.json..."
cd "${REPO_ROOT}/${PROJECT_NAME}/src/${PROJECT_NAME}"
cat > aws-lambda-tools-defaults.json << EOF
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
  "function-handler": "${PROJECT_NAME}::${PROJECT_NAME}.Function::FunctionHandler",
  "function-name": "Splity${ACTION}${ENTITY}",
  "environment-variables": {
    "CLUSTER_HOSTNAME": "",
    "CLUSTER_USERNAME": "admin",
    "CLUSTER_DATABASE": "postgres",
    "AWS_BUCKET_NAME": "split-app-v1",
    "AWS_BUCKET_REGION": "eu-central-1",
    "ALLOWED_ORIGINS": "*"
  }
}
EOF

# Step 7: Build and test
echo "Step 7: Building and testing..."
cd "${REPO_ROOT}"
dotnet build
dotnet test "${PROJECT_NAME}/test/${PROJECT_NAME}.Tests/"

echo "✅ Lambda function ${PROJECT_NAME} created successfully!"
echo "📁 Project location: ${REPO_ROOT}/${PROJECT_NAME}"
echo "🏗️  Function name will be: Splity${ACTION}${ENTITY}"