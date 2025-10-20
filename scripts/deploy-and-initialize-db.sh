#!/bin/bash

# Splity Post-Deployment Database Initialization Script
# This script deploys the infrastructure and then initializes the database schema
#
# Usage:
#   ./deploy-and-initialize-db.sh [environment] [region] [--skip-db]
#   ./deploy-and-initialize-db.sh --skip-db [environment] [region]
#
# Parameters:
#   environment  - AWS environment (default: dev)
#   region      - AWS region (default: eu-west-2)
#   --skip-db   - Skip database deployment and initialization
#
# Examples:
#   ./deploy-and-initialize-db.sh dev eu-west-2
#   ./deploy-and-initialize-db.sh --skip-db dev eu-west-2
#   ./deploy-and-initialize-db.sh prod eu-central-1 --skip-db

set -e  # Exit on any error

# Configuration
ENVIRONMENT=${1:-dev}
REGION=${2:-eu-west-2}
SKIP_DB=${3:-false}
STACK_NAME="splity-complete-infrastructure-${ENVIRONMENT}"

if [[ "$SKIP_DB" == "true" ]]; then
    echo "🚀 Starting Splity deployment (skipping database) for environment: ${ENVIRONMENT}"
else
    echo "🚀 Starting Splity deployment and database initialization for environment: ${ENVIRONMENT}"
fi

if [[ "$SKIP_DB" != "true" ]]; then
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
    
    cd ../../../
else
    echo "⏭️  Skipping database deployment and initialization"
fi

# Step 4: Deploy all other Lambda functions
echo "🔧 Deploying all Lambda functions in parallel..."

# Get the current directory for absolute path references
ROOT_DIR=$(pwd)

# Deploy functions in parallel - each in its own subshell with proper working directory
echo "Starting Party function deployments..."
(cd "$ROOT_DIR/Splity.Party.Create/src/Splity.Party.Create" && echo "Deploying SplityCreateParty-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityCreateParty-${ENVIRONMENT} --region ${REGION}) &
PID_PARTY_CREATE=$!

(cd "$ROOT_DIR/Splity.Party.Get/src/Splity.Party.Get" && echo "Deploying SplityGetParty-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityGetParty-${ENVIRONMENT} --region ${REGION}) &
PID_PARTY_GET=$!

(cd "$ROOT_DIR/Splity.Party.Update/src/Splity.Party.Update" && echo "Deploying SplityUpdateParty-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityUpdateParty-${ENVIRONMENT} --region ${REGION}) &
PID_PARTY_UPDATE=$!

(cd "$ROOT_DIR/Splity.Party.Delete/src/Splity.Party.Delete" && echo "Deploying SplityDeleteParty-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityDeleteParty-${ENVIRONMENT} --region ${REGION}) &
PID_PARTY_DELETE=$!

(cd "$ROOT_DIR/Splity.Parties.Get/src/Splity.Parties.Get" && echo "Deploying SplityGetParties-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityGetParties-${ENVIRONMENT} --region ${REGION}) &
PID_PARTIES_GET=$!

echo "Starting Expense function deployments..."
(cd "$ROOT_DIR/Splity.Expenses.Create/src/Splity.Expenses.Create" && echo "Deploying SplityCreateExpenses-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityCreateExpenses-${ENVIRONMENT} --region ${REGION}) &
PID_EXPENSES_CREATE=$!

(cd "$ROOT_DIR/Splity.Expenses.Delete/src/Splity.Expenses.Delete" && echo "Deploying SplityDeleteExpenses-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityDeleteExpenses-${ENVIRONMENT} --region ${REGION}) &
PID_EXPENSES_DELETE=$!

(cd "$ROOT_DIR/Splity.Expenses.Extract/src/Splity.Expenses.Extract" && echo "Deploying SplityExtractExpenses-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityExtractExpenses-${ENVIRONMENT} --region ${REGION}) &
PID_EXPENSES_EXTRACT=$!

echo "Starting User function deployments..."
(cd "$ROOT_DIR/Splity.User.Create/src/Splity.User.Create" && echo "Deploying SplityCreateUser-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityCreateUser-${ENVIRONMENT} --region ${REGION}) &
PID_USER_CREATE=$!

(cd "$ROOT_DIR/Splity.User.Get/src/Splity.User.Get" && echo "Deploying SplityGetUser-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityGetUser-${ENVIRONMENT} --region ${REGION}) &
PID_USER_GET=$!

(cd "$ROOT_DIR/Splity.User.Update/src/Splity.User.Update" && echo "Deploying SplityUpdateUser-${ENVIRONMENT}..." && dotnet lambda deploy-function --function-name SplityUpdateUser-${ENVIRONMENT} --region ${REGION}) &
PID_USER_UPDATE=$!

echo "Waiting for all deployments to complete..."
# Store all PIDs for tracking
DEPLOYMENT_PIDS=($PID_PARTY_CREATE $PID_PARTY_GET $PID_PARTY_UPDATE $PID_PARTY_DELETE $PID_EXPENSES_CREATE $PID_EXPENSES_DELETE $PID_EXPENSES_EXTRACT $PID_USER_CREATE $PID_USER_GET $PID_USER_UPDATE)
FAILED_DEPLOYMENTS=()

# Wait for each deployment and track failures
for i in "${!DEPLOYMENT_PIDS[@]}"; do
    PID=${DEPLOYMENT_PIDS[$i]}
    if ! wait $PID; then
        case $i in
            0) FAILED_DEPLOYMENTS+=("SplityCreateParty-${ENVIRONMENT}") ;;
            1) FAILED_DEPLOYMENTS+=("SplityGetParty-${ENVIRONMENT}") ;;
            2) FAILED_DEPLOYMENTS+=("SplityUpdateParty-${ENVIRONMENT}") ;;
            3) FAILED_DEPLOYMENTS+=("SplityDeleteParty-${ENVIRONMENT}") ;;
            4) FAILED_DEPLOYMENTS+=("SplityCreateExpenses-${ENVIRONMENT}") ;;
            5) FAILED_DEPLOYMENTS+=("SplityDeleteExpenses-${ENVIRONMENT}") ;;
            6) FAILED_DEPLOYMENTS+=("SplityExtractExpenses-${ENVIRONMENT}") ;;
            7) FAILED_DEPLOYMENTS+=("SplityCreateUser-${ENVIRONMENT}") ;;
            8) FAILED_DEPLOYMENTS+=("SplityGetUser-${ENVIRONMENT}") ;;
            9) FAILED_DEPLOYMENTS+=("SplityUpdateUser-${ENVIRONMENT}") ;;
            10)FAILED_DEPLOYMENTS+=("SplityPartiesGet-${ENVIRONMENT}") ;;
        esac
    fi
done

# Report deployment results
if [ ${#FAILED_DEPLOYMENTS[@]} -eq 0 ]; then
    echo "✅ All Lambda functions deployed successfully!"
else
    echo "⚠️  Some deployments failed:"
    for failed_function in "${FAILED_DEPLOYMENTS[@]}"; do
        echo "   - $failed_function"
    done
    echo "❌ Deployment completed with ${#FAILED_DEPLOYMENTS[@]} failures"
    exit 1
fi

if [[ "$SKIP_DB" == "true" ]]; then
    echo "🎉 Deployment completed successfully (database skipped)!"
else
    echo "🎉 Deployment and database initialization completed successfully!"
fi

echo "📋 Next steps:"
echo "   1. Verify Lambda functions: aws lambda list-functions --query 'Functions[?starts_with(FunctionName, \`Splity\`)].FunctionName' --region ${REGION}"
echo "   2. Test API endpoints using the CloudFormation stack outputs"
echo "   3. Check CloudWatch logs for any issues"

if [[ "$SKIP_DB" == "true" ]]; then
    echo "   4. Note: Database was skipped - you may need to initialize it separately"
fi

# Cleanup temp file (only if it exists)
if [[ "$SKIP_DB" != "true" ]]; then
    rm -f /tmp/db-init-response.json
fi
