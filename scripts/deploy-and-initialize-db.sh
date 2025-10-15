#!/bin/bash

# Splity Post-Deployment Database Initialization Script
# This script deploys the infrastructure and then initializes the database schema

set -e  # Exit on any error

# Configuration
ENVIRONMENT=${1:-dev}
REGION=${2:-eu-west-2}
STACK_NAME="splity-complete-infrastructure-${ENVIRONMENT}"

echo "🚀 Starting Splity deployment and database initialization for environment: ${ENVIRONMENT}"

# Step 1: Deploy CloudFormation stack
echo "📦 Deploying CloudFormation infrastructure..."
aws cloudformation deploy \
  --template-file splity-infrastructure-cf-template.yaml \
  --stack-name ${STACK_NAME} \
  --parameter-overrides Environment=${ENVIRONMENT} \
  --capabilities CAPABILITY_NAMED_IAM \
  --region ${REGION}

# Step 2: Deploy Database Initialize Lambda
echo "🏗️  Deploying Database Initialize Lambda function..."
cd Splity.Database.Initialize/src/Splity.Database.Initialize
dotnet lambda deploy-function \
  --function-name "SplityDatabaseInitialize-${ENVIRONMENT}" \
  --region ${REGION} \
  --function-role "SplityPartyLambdaRole-${ENVIRONMENT}"

# Step 3: Invoke Database Initialize Lambda to run schema
echo "🗄️  Initializing database schema..."
aws lambda invoke \
  --function-name "SplityDatabaseInitialize-${ENVIRONMENT}" \
  --region ${REGION} \
  --payload '{"httpMethod":"GET","path":"/initialize","headers":{}}' \
  /tmp/db-init-response.json

# Check if initialization was successful
if grep -q '"statusCode":200' /tmp/db-init-response.json; then
    echo "✅ Database schema initialized successfully!"
    cat /tmp/db-init-response.json | jq .
else
    echo "❌ Database initialization failed!"
    cat /tmp/db-init-response.json
    exit 1
fi

# Step 4: Deploy all other Lambda functions
cd ../../../
echo "🔧 Deploying all Lambda functions..."

# Deploy functions in parallel for faster deployment
{
    echo "Deploying Party functions..."
    cd Splity.Party.Create/src/Splity.Party.Create && dotnet lambda deploy-function --function-name SplityCreateParty-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.Party.Get/src/Splity.Party.Get && dotnet lambda deploy-function --function-name SplityGetParty-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.Party.Update/src/Splity.Party.Update && dotnet lambda deploy-function --function-name SplityUpdateParty-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.Party.Delete/src/Splity.Party.Delete && dotnet lambda deploy-function --function-name SplityDeleteParty-${ENVIRONMENT} --region ${REGION} &
} &

{
    echo "Deploying Expense functions..."
    cd Splity.Expenses.Create/src/Splity.Expenses.Create && dotnet lambda deploy-function --function-name SplityCreateExpenses-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.Expenses.Delete/src/Splity.Expenses.Delete && dotnet lambda deploy-function --function-name SplityDeleteExpenses-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.Expenses.Extract/src/Splity.Expenses.Extract && dotnet lambda deploy-function --function-name SplityExtractExpenses-${ENVIRONMENT} --region ${REGION} &
} &

{
    echo "Deploying User functions..."
    cd Splity.User.Create/src/Splity.User.Create && dotnet lambda deploy-function --function-name SplityCreateUser-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.User.Get/src/Splity.User.Get && dotnet lambda deploy-function --function-name SplityGetUser-${ENVIRONMENT} --region ${REGION} &
    cd ../../../Splity.User.Update/src/Splity.User.Update && dotnet lambda deploy-function --function-name SplityUpdateUser-${ENVIRONMENT} --region ${REGION} &
} &

# Wait for all background jobs to complete
wait

echo "🎉 Deployment and database initialization completed successfully!"
echo "📋 Next steps:"
echo "   1. Verify Lambda functions: aws lambda list-functions --query 'Functions[?starts_with(FunctionName, \`Splity\`)].FunctionName' --region ${REGION}"
echo "   2. Test API endpoints using the CloudFormation stack outputs"
echo "   3. Check CloudWatch logs for any issues"

# Cleanup temp file
rm -f /tmp/db-init-response.json