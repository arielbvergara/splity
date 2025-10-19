# Next.js Cognito Authentication Setup Guide

This guide explains how to set up AWS Cognito authentication in the Splity Next.js application.

## Prerequisites

- AWS Cognito User Pool already deployed (follow the main COGNITO_SETUP.md first)
- Next.js application running (splity-web folder)
- Yarn or npm installed

## Setup Steps

### 1. Install Dependencies

The required packages have been installed:
- `oidc-client-ts` - OpenID Connect client library
- `react-oidc-context` - React context for OIDC

### 2. Configure Environment Variables

Update `.env.local` with your Cognito configuration:

```bash
# AWS Cognito Configuration
NEXT_PUBLIC_COGNITO_AUTHORITY=https://cognito-idp.eu-west-2.amazonaws.com/eu-west-2_YOUR_USER_POOL_ID
NEXT_PUBLIC_COGNITO_CLIENT_ID=YOUR_CLIENT_ID
NEXT_PUBLIC_COGNITO_DOMAIN=https://your-domain.auth.eu-west-2.amazoncognito.com
NEXT_PUBLIC_COGNITO_REDIRECT_URI=http://localhost:3000/callback
NEXT_PUBLIC_COGNITO_LOGOUT_URI=http://localhost:3000

# Existing API configuration
NEXT_PUBLIC_API_BASE_URL=https://your-api-gateway-url.execute-api.eu-west-2.amazonaws.com/dev
```

**Important**: Replace the placeholder values with your actual Cognito configuration from the CloudFormation deployment.

### 3. Architecture Overview

The authentication system includes:

#### Components Created:
- `contexts/cognito-auth-context.tsx` - Main authentication provider using OIDC
- `lib/authenticated-api-client.ts` - API client that automatically includes JWT tokens
- `components/auth-initializer.tsx` - Sets up the API client with token getter
- `app/callback/page.tsx` - OAuth callback handler

#### Components Updated:
- `app/layout.tsx` - Uses CognitoAuthProvider instead of AuthProvider
- `components/header.tsx` - Shows user menu and sign in/out buttons
- `app/dashboard/page.tsx` - Protected route requiring authentication
- `types/index.ts` - Added cognitoUserId field to User type
- `services/user-service.ts` - Uses authenticated API client

### 4. Authentication Flow

1. **Unauthenticated User**:
   - Visits homepage
   - Clicks "Sign In" button
   - Redirected to Cognito Hosted UI
   - Completes sign up/sign in

2. **Authentication Process**:
   - Cognito redirects to `/callback` with authorization code
   - OIDC client exchanges code for JWT tokens
   - Frontend stores tokens securely
   - User information extracted from JWT

3. **User Management**:
   - Frontend calls `/users` endpoint with JWT token
   - Backend validates JWT token
   - If user doesn't exist, creates new user with Cognito ID
   - Returns user information for frontend

4. **API Calls**:
   - All API calls include `Authorization: Bearer {access_token}` header
   - Backend Lambda functions validate JWT tokens
   - User ID automatically extracted for business logic

### 5. Backend Integration

The backend Lambda functions have been updated with:

- **Authentication Library** (`Splity.Shared.Authentication`):
  - JWT token validation
  - User lookup by Cognito ID
  - Automatic user creation

- **Updated Lambda Functions**:
  - `Splity.Party.Create` - Uses authenticated user ID
  - Other functions can be updated following the same pattern

- **Database Changes**:
  - `CognitoUserId` column added to Users table
  - Indexes for efficient lookups

### 6. Protected Routes

Routes that require authentication:
- `/dashboard` - Main dashboard (automatically redirects if not authenticated)
- `/dashboard/analytics` - Analytics page
- Any other dashboard routes

### 7. Development Testing

To test the authentication flow:

1. **Start the development server**:
   ```bash
   cd splity-web
   yarn dev
   ```

2. **Visit `http://localhost:3000`**:
   - Should show homepage with "Sign In" button
   - Navigation links only visible when authenticated

3. **Click "Sign In"**:
   - Redirected to Cognito Hosted UI
   - Can create new account or sign in with existing

4. **After authentication**:
   - Redirected back to application
   - Header shows user dropdown menu
   - Dashboard accessible with user data

5. **Test API Integration**:
   - Create a new party (should use your authenticated user ID)
   - Check browser network tab for Authorization headers

### 8. Production Configuration

For production deployment:

1. **Update redirect URLs in Cognito**:
   - Add production domain to callback URLs
   - Update environment variables with production URLs

2. **Security considerations**:
   - Ensure HTTPS for production
   - Configure proper CORS settings
   - Set secure cookie flags

3. **Environment variables**:
   ```bash
   # Production environment
   NEXT_PUBLIC_COGNITO_REDIRECT_URI=https://yourdomain.com/callback
   NEXT_PUBLIC_COGNITO_LOGOUT_URI=https://yourdomain.com
   ```

### 9. Troubleshooting

#### Common Issues:

1. **"Configuration Error"**:
   - Check that all environment variables are set correctly
   - Verify Cognito User Pool ID and Client ID

2. **"Invalid Redirect URI"**:
   - Ensure redirect URI in env matches Cognito configuration
   - Check both local and production URLs

3. **API calls failing with 401**:
   - Check that backend Lambda functions have environment variables set
   - Verify JWT token validation is working

4. **User creation failing**:
   - Check database has CognitoUserId column
   - Verify CreateUserRequest includes cognitoUserId field

#### Debug Information:

Enable debug logging by checking:
- Browser developer console for frontend errors
- CloudWatch logs for Lambda function errors
- Network tab for API request/response details

### 10. Key Benefits

This implementation provides:

- **Secure Authentication**: AWS Cognito handles security best practices
- **Automatic User Management**: Users created seamlessly during first login
- **JWT Token Validation**: Backend validates tokens for each request
- **Scalable Architecture**: Supports multiple frontend clients
- **Backwards Compatible**: Existing API contracts maintained

The authentication system is now ready for production use with proper user management and security!