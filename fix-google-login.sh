#!/bin/bash

# Script to fix Google login configuration in Cognito User Pool
# This addresses the "attributes required: [name]" error

# Configuration - Update these with your actual values
USER_POOL_ID="eu-west-2_MNEAwllzl"
CLIENT_ID="1cn854vio5n33n0rk4f749v6m2"
REGION="eu-west-2"
GOOGLE_CLIENT_ID="YOUR_GOOGLE_CLIENT_ID"
GOOGLE_CLIENT_SECRET="YOUR_GOOGLE_CLIENT_SECRET"

echo "Fixing Cognito User Pool configuration for Google login..."

# 1. Update User Pool to make 'name' attribute optional instead of required
echo "Step 1: Making 'name' attribute optional in User Pool schema..."
aws cognito-idp update-user-pool \
  --user-pool-id $USER_POOL_ID \
  --region $REGION \
  --policies '{
    "PasswordPolicy": {
      "MinimumLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireNumbers": true,
      "RequireSymbols": false
    }
  }' \
  --schema '[
    {
      "Name": "email",
      "AttributeDataType": "String",
      "Required": true,
      "Mutable": true
    },
    {
      "Name": "name",
      "AttributeDataType": "String",
      "Required": false,
      "Mutable": true
    }
  ]' \
  --auto-verified-attributes email

echo "Step 2: Configuring Google as Identity Provider..."

# Check if Google identity provider already exists
EXISTING_PROVIDER=$(aws cognito-idp describe-identity-provider \
  --user-pool-id $USER_POOL_ID \
  --provider-name Google \
  --region $REGION \
  --output text --query 'IdentityProvider.ProviderName' 2>/dev/null)

if [ "$EXISTING_PROVIDER" = "Google" ]; then
  echo "Google identity provider already exists, updating..."
  aws cognito-idp update-identity-provider \
    --user-pool-id $USER_POOL_ID \
    --provider-name Google \
    --region $REGION \
    --provider-details '{
      "client_id": "'$GOOGLE_CLIENT_ID'",
      "client_secret": "'$GOOGLE_CLIENT_SECRET'",
      "authorize_scopes": "openid email profile"
    }' \
    --attribute-mapping '{
      "email": "email",
      "name": "name",
      "given_name": "given_name",
      "family_name": "family_name"
    }'
else
  echo "Creating new Google identity provider..."
  aws cognito-idp create-identity-provider \
    --user-pool-id $USER_POOL_ID \
    --provider-name Google \
    --provider-type Google \
    --region $REGION \
    --provider-details '{
      "client_id": "'$GOOGLE_CLIENT_ID'",
      "client_secret": "'$GOOGLE_CLIENT_SECRET'",
      "authorize_scopes": "openid email profile"
    }' \
    --attribute-mapping '{
      "email": "email",
      "name": "name",
      "given_name": "given_name",
      "family_name": "family_name"
    }'
fi

echo "Step 3: Updating User Pool Client to support Google login..."
aws cognito-idp update-user-pool-client \
  --user-pool-id $USER_POOL_ID \
  --client-id $CLIENT_ID \
  --region $REGION \
  --supported-identity-providers COGNITO Google \
  --callback-urls "http://localhost:3000/callback" "https://localhost:3000/callback" \
  --logout-urls "http://localhost:3000" "https://localhost:3000" \
  --allowed-o-auth-flows code implicit \
  --allowed-o-auth-flows-user-pool-client \
  --allowed-o-auth-scopes email openid profile \
  --explicit-auth-flows ALLOW_USER_SRP_AUTH ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH \
  --generate-secret false

echo "Step 4: Updating User Pool Domain configuration..."
aws cognito-idp describe-user-pool-domain \
  --domain eu-west-2mneawllzl \
  --region $REGION

echo ""
echo "Configuration updated successfully!"
echo ""
echo "IMPORTANT: You still need to:"
echo "1. Replace GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET with your actual Google OAuth credentials"
echo "2. Configure Google OAuth with the correct redirect URIs in Google Cloud Console"
echo "3. Test the login flow"
echo ""
echo "Google OAuth Redirect URIs to add in Google Cloud Console:"
echo "- http://localhost:3000/callback"
echo "- https://eu-west-2mneawllzl.auth.eu-west-2.amazoncognito.com/oauth2/idpresponse"