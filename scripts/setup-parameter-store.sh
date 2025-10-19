#!/bin/bash

# AWS Parameter Store Setup Script for Splity Application
# This script creates or updates parameters in AWS Systems Manager Parameter Store
# Usage: ./setup-parameter-store.sh [environment] [region]
# Example: ./setup-parameter-store.sh dev eu-west-2

set -e

# Default values
ENVIRONMENT=${1:-"dev"}
REGION=${2:-"eu-west-2"}

echo "Setting up Parameter Store for Splity application..."
echo "Environment: $ENVIRONMENT"
echo "Region: $REGION"
echo ""

# Function to create or update a parameter
create_or_update_parameter() {
    local name="$1"
    local value="$2"
    local type="$3"
    local description="$4"
    
    echo "Processing parameter: $name"
    
    # Check if parameter exists
    if aws ssm get-parameter --name "$name" --region "$REGION" >/dev/null 2>&1; then
        echo "  Parameter exists, updating..."
        aws ssm put-parameter \
            --name "$name" \
            --value "$value" \
            --type "$type" \
            --description "$description" \
            --overwrite \
            --region "$REGION"
        echo "  ✓ Updated"
    else
        echo "  Parameter doesn't exist, creating..."
        aws ssm put-parameter \
            --name "$name" \
            --value "$value" \
            --type "$type" \
            --description "$description" \
            --region "$REGION" \
            --tags "Key=Environment,Value=$ENVIRONMENT" "Key=Service,Value=Splity"
        echo "  ✓ Created"
    fi
    echo ""
}

# Database Configuration
echo "📊 Setting up Database Configuration..."
create_or_update_parameter "/splity/$ENVIRONMENT/database/username" "admin" "String" "Database username for $ENVIRONMENT environment"
create_or_update_parameter "/splity/$ENVIRONMENT/database/name" "postgres" "String" "Database name for $ENVIRONMENT environment"
create_or_update_parameter "/splity/$ENVIRONMENT/database/region" "$REGION" "String" "Database region for $ENVIRONMENT environment"

# Note: Database hostname will be set by CloudFormation after DSQL cluster creation
echo "ℹ️  Database hostname will be automatically set by CloudFormation after DSQL cluster creation"
echo ""

# AWS Configuration
echo "☁️  Setting up AWS Configuration..."
create_or_update_parameter "/splity/$ENVIRONMENT/aws/bucket/name" "split-app-v1" "String" "AWS S3 bucket name for $ENVIRONMENT environment"
create_or_update_parameter "/splity/$ENVIRONMENT/aws/bucket/region" "eu-central-1" "String" "AWS S3 bucket region for $ENVIRONMENT environment"
create_or_update_parameter "/splity/$ENVIRONMENT/aws/region" "$REGION" "String" "AWS region for $ENVIRONMENT environment"

# Azure Configuration
echo "🔵 Setting up Azure Configuration..."
read -p "Enter Azure Document Intelligence endpoint [https://di-document-reader-test.cognitiveservices.azure.com/]: " azure_endpoint
azure_endpoint=${azure_endpoint:-"https://di-document-reader-test.cognitiveservices.azure.com/"}
create_or_update_parameter "/splity/$ENVIRONMENT/azure/document-intelligence/endpoint" "$azure_endpoint" "String" "Azure Document Intelligence endpoint for $ENVIRONMENT environment"

echo "🔐 Please enter your Azure Document Intelligence API key (this will be stored as SecureString):"
read -s azure_api_key
if [ ! -z "$azure_api_key" ]; then
    create_or_update_parameter "/splity/$ENVIRONMENT/azure/document-intelligence/api-key" "$azure_api_key" "SecureString" "Azure Document Intelligence API key for $ENVIRONMENT environment (encrypted)"
else
    echo "❌ No API key provided. Please set this parameter manually later."
fi
echo ""

# Cognito Configuration (optional)
echo "🔐 Setting up Cognito Configuration (optional)..."
read -p "Enter Cognito User Pool ID (leave empty to skip): " cognito_pool_id
if [ ! -z "$cognito_pool_id" ]; then
    create_or_update_parameter "/splity/$ENVIRONMENT/cognito/user-pool-id" "$cognito_pool_id" "String" "Cognito User Pool ID for $ENVIRONMENT environment"
fi

read -p "Enter Cognito Client ID (leave empty to skip): " cognito_client_id
if [ ! -z "$cognito_client_id" ]; then
    create_or_update_parameter "/splity/$ENVIRONMENT/cognito/client-id" "$cognito_client_id" "String" "Cognito Client ID for $ENVIRONMENT environment"
fi

# Application Configuration
echo "⚙️  Setting up Application Configuration..."
read -p "Enter allowed CORS origins [*]: " allowed_origins
allowed_origins=${allowed_origins:-"*"}
create_or_update_parameter "/splity/$ENVIRONMENT/application/allowed-origins" "$allowed_origins" "String" "CORS allowed origins for $ENVIRONMENT environment"

echo ""
echo "🎉 Parameter Store setup completed!"
echo ""
echo "📋 Summary of created/updated parameters:"
echo "   /splity/$ENVIRONMENT/database/username"
echo "   /splity/$ENVIRONMENT/database/name"  
echo "   /splity/$ENVIRONMENT/database/region"
echo "   /splity/$ENVIRONMENT/aws/bucket/name"
echo "   /splity/$ENVIRONMENT/aws/bucket/region"
echo "   /splity/$ENVIRONMENT/aws/region"
echo "   /splity/$ENVIRONMENT/azure/document-intelligence/endpoint"
echo "   /splity/$ENVIRONMENT/azure/document-intelligence/api-key (SecureString)"
if [ ! -z "$cognito_pool_id" ]; then
    echo "   /splity/$ENVIRONMENT/cognito/user-pool-id"
fi
if [ ! -z "$cognito_client_id" ]; then
    echo "   /splity/$ENVIRONMENT/cognito/client-id"
fi
echo "   /splity/$ENVIRONMENT/application/allowed-origins"
echo ""
echo "✅ You can now deploy your Lambda functions with Parameter Store integration!"
echo ""
echo "🔍 To verify parameters were created, run:"
echo "   aws ssm get-parameters-by-path --path '/splity/$ENVIRONMENT/' --recursive --region $REGION"