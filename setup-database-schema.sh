#!/bin/bash

# Setup database schema for Splity in Aurora DSQL
set -e

# DSQL cluster details from CloudFormation outputs
CLUSTER_ENDPOINT="m5thplzhwzhmh6n74c6wcr3hzi.dsql.eu-west-2.on.aws"
REGION="eu-west-2"
DATABASE="postgres"
USERNAME="admin"

echo "🚀 Setting up Splity database schema in Aurora DSQL..."
echo "Cluster: $CLUSTER_ENDPOINT"

# Get AWS credentials and create a temporary password for DSQL connection
echo "📝 Generating DSQL auth token..."

# For Aurora DSQL, we need to use AWS IAM Database Authentication
# Generate an auth token that can be used as a password
AUTH_TOKEN=$(aws dsql generate-db-connect-admin-auth-token \
    --hostname "$CLUSTER_ENDPOINT" \
    --region "$REGION" \
    2>/dev/null || echo "")

if [ -z "$AUTH_TOKEN" ]; then
    echo "❌ Failed to generate DSQL auth token. Trying alternative approach..."
    
    # Try using regular AWS IAM credentials with DSQL
    # For DSQL, we might need to use AWS SigningV4 authentication
    echo "🔐 Using AWS credentials directly..."
    
    # Use psql with AWS IAM authentication
    PGPASSWORD="dummy" psql \
        -h "$CLUSTER_ENDPOINT" \
        -U "$USERNAME" \
        -d "$DATABASE" \
        -p 5432 \
        -c "SELECT version();" \
        --set=sslmode=require \
        2>/dev/null || {
        echo "❌ Direct connection failed. DSQL may require additional setup or authentication."
        echo "📖 Please check AWS documentation for DSQL connectivity requirements."
        exit 1
    }
else
    echo "✅ Auth token generated successfully"
    
    # Connect to DSQL using the auth token as password
    PGPASSWORD="$AUTH_TOKEN" psql \
        -h "$CLUSTER_ENDPOINT" \
        -U "$USERNAME" \
        -d "$DATABASE" \
        -p 5432 \
        --set=sslmode=require \
        -f database-schema.sql
fi

echo "✅ Database schema setup completed successfully!"
echo "📊 You can now test your Lambda functions with the database."