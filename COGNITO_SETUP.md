# AWS Cognito Authentication Setup for Splity

This guide walks you through setting up AWS Cognito authentication for your Splity application.

## Prerequisites

- AWS CLI configured with appropriate credentials
- .NET 8 SDK installed
- PostgreSQL database access
- AWS Lambda Tools installed: `dotnet tool install -g Amazon.Lambda.Tools`

## Step 1: Deploy Cognito Infrastructure

Deploy the Cognito User Pool and related resources:

```bash
# Deploy Cognito infrastructure
aws cloudformation deploy \
  --template-file cognito-infrastructure-cf-template.yaml \
  --stack-name splity-cognito-dev \
  --parameter-overrides \
    Environment=dev \
    DomainPrefix=splity-dev \
    CallbackURL=http://localhost:8080/callback \
    LogoutURL=http://localhost:8080/ \
  --region eu-west-2

# Get the outputs from the deployment
aws cloudformation describe-stacks \
  --stack-name splity-cognito-dev \
  --region eu-west-2 \
  --query 'Stacks[0].Outputs'
```

Save the following values from the outputs:
- **UserPoolId**: Copy this for environment variables
- **UserPoolClientId**: Copy this for frontend and environment variables  
- **UserPoolDomain**: This is your Cognito hosted UI domain
- **Region**: AWS region (eu-west-2)

## Step 2: Update Database Schema

Run the database migration to add the CognitoUserId column:

```bash
# Connect to your PostgreSQL database and run:
psql -h YOUR_CLUSTER_HOSTNAME -U admin -d postgres -f database/add_cognito_user_id.sql
```

Alternatively, run the SQL directly:
```sql
ALTER TABLE Users ADD COLUMN CognitoUserId VARCHAR(128);
CREATE INDEX idx_users_cognito_user_id ON Users(CognitoUserId);
```

## Step 3: Update Lambda Functions

### Add Environment Variables

Update your Lambda functions to include Cognito configuration. Add these environment variables to all Lambda functions that need authentication:

```bash
# For each Lambda function, add these environment variables:
export COGNITO_USER_POOL_ID="your-user-pool-id"
export COGNITO_CLIENT_ID="your-client-id"  
export AWS_REGION="eu-west-2"
```

You can update environment variables for deployed functions using:

```bash
aws lambda update-function-configuration \
  --function-name SplityCreateParty-dev \
  --region eu-west-2 \
  --environment Variables="{COGNITO_USER_POOL_ID=your-user-pool-id,COGNITO_CLIENT_ID=your-client-id,AWS_REGION=eu-west-2,CLUSTER_HOSTNAME=your-cluster-hostname,CLUSTER_USERNAME=admin,CLUSTER_DATABASE=postgres,AWS_BUCKET_NAME=split-app-v1,AWS_BUCKET_REGION=eu-central-1,ALLOWED_ORIGINS=*}"
```

### Update Function Code

The Party.Create function has already been updated to use authentication. For other functions, follow this pattern:

1. Add authentication library reference:
```bash
dotnet add reference ../../../Splity.Shared.Authentication/Splity.Shared.Authentication.csproj
```

2. Update the function class to inherit from `BaseAuthenticatedLambdaFunction`
3. Add authentication check at the start of `FunctionHandler`

### Deploy Updated Functions

```bash
# Build and deploy Party.Create function with authentication
cd Splity.Party.Create/src/Splity.Party.Create
dotnet lambda deploy-function --function-name SplityCreateParty-dev --region eu-west-2

# For other functions, follow similar pattern after updating them
```

## Step 4: Update Frontend Configuration

Update the frontend authentication file with your Cognito details:

Edit `frontend/auth.html` and replace the configuration section:

```javascript
const CONFIG = {
    region: 'eu-west-2',
    userPoolId: 'YOUR_USER_POOL_ID',           // From CloudFormation output
    clientId: 'YOUR_CLIENT_ID',                 // From CloudFormation output  
    apiUrl: 'https://your-api-id.execute-api.eu-west-2.amazonaws.com' // Your API Gateway URL
};
```

## Step 5: Test the Authentication Flow

1. **Open the frontend**: Open `frontend/auth.html` in your browser
2. **Create an account**: 
   - Click "Sign Up"
   - Enter name, email, and password
   - Check your email for confirmation code
   - Enter confirmation code
3. **Sign in**:
   - Enter email and password
   - Should see welcome screen with user info
4. **Test API**:
   - Click "Test Authenticated API" button
   - Should successfully create a party using your authenticated user ID

## Step 6: Update API Gateway (Optional)

If you want to use Cognito authorizers at the API Gateway level:

1. **Update API Gateway Configuration**:
   - Add Cognito User Pool Authorizer to your API Gateway
   - Configure routes to require authentication

2. **Example CloudFormation for API Gateway Authorizer**:
```yaml
CognitoAuthorizer:
  Type: AWS::ApiGateway::Authorizer
  Properties:
    Name: SplityCognitoAuthorizer
    Type: COGNITO_USER_POOLS
    IdentitySource: method.request.header.Authorization
    RestApiId: !Ref SplityApiGateway
    ProviderARNs:
      - !GetAtt SplityUserPool.Arn
```

## Authentication Flow Overview

Here's how the authentication works:

### Frontend Flow:
1. User signs up/signs in via Cognito
2. Cognito returns JWT tokens (access, ID, refresh)
3. Frontend stores tokens in secure cookies
4. API requests include `Authorization: Bearer <access-token>` header

### Backend Flow:
1. Lambda function extends `BaseAuthenticatedLambdaFunction`
2. `AuthenticateAsync()` extracts and validates JWT token
3. If valid, user info is loaded/created in database
4. Function proceeds with `CurrentUserId` available
5. If invalid, returns 401 Unauthorized

### Database Integration:
1. JWT contains user email and name
2. System looks up user by email in database
3. If user doesn't exist, creates new user record
4. Links Cognito sub (user ID) with internal user ID
5. Returns internal user ID for business logic

## Environment Variables Summary

All Lambda functions need these environment variables:

```bash
# Cognito Configuration
COGNITO_USER_POOL_ID=us-west-2_XXXXXXXXX
COGNITO_CLIENT_ID=7example23exampleexample
AWS_REGION=eu-west-2

# Existing variables (keep these)
CLUSTER_HOSTNAME=your-cluster-hostname
CLUSTER_USERNAME=admin  
CLUSTER_DATABASE=postgres
AWS_BUCKET_NAME=split-app-v1
AWS_BUCKET_REGION=eu-central-1
ALLOWED_ORIGINS=*
```

## Security Considerations

1. **HTTPS Only**: Use HTTPS in production for token transmission
2. **Token Storage**: Store tokens in secure, httpOnly cookies in production
3. **Token Refresh**: Implement automatic token refresh logic
4. **CORS**: Configure proper CORS settings for your domain
5. **Rate Limiting**: Consider implementing rate limiting on authentication endpoints

## Troubleshooting

### Common Issues:

1. **JWT Validation Fails**:
   - Check User Pool ID and Client ID are correct
   - Verify region matches
   - Ensure token hasn't expired

2. **Database Connection Issues**:
   - Verify database schema has CognitoUserId column
   - Check database connection string and permissions

3. **CORS Issues**:
   - Update ALLOWED_ORIGINS environment variable
   - Ensure API Gateway has proper CORS configuration

4. **Frontend Issues**:
   - Check browser console for errors
   - Verify Cognito configuration values
   - Test with different browsers

### Debug Mode:

Enable debug logging in Lambda functions by setting:
```bash
LAMBDA_LOG_LEVEL=DEBUG
```

This will provide detailed authentication flow logging.