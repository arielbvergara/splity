# Fix Google Login "attributes required: [name]" Error

The error you're seeing indicates that your Cognito User Pool requires the `name` attribute, but Google login isn't providing it in the expected format. Here's how to fix it:

## Quick Fix Steps:

### 1. Update your redirect URI (DONE)
I've already updated your `.env.local` to use the correct callback URL: `http://localhost:3000/callback`

### 2. Check your Cognito User Pool Configuration

First, let's see what's currently configured:

```bash
# Check current User Pool configuration
aws cognito-idp describe-user-pool \
  --user-pool-id eu-west-2_MNEAwllzl \
  --region eu-west-2 \
  --query 'UserPool.Schema[?Name==`name`]'

# Check User Pool Client configuration
aws cognito-idp describe-user-pool-client \
  --user-pool-id eu-west-2_MNEAwllzl \
  --client-id 1cn854vio5n33n0rk4f749v6m2 \
  --region eu-west-2
```

### 3. Fix the Callback URLs

Update your Cognito User Pool Client with correct callback URLs:

```bash
aws cognito-idp update-user-pool-client \
  --user-pool-id eu-west-2_MNEAwllzl \
  --client-id 1cn854vio5n33n0rk4f749v6m2 \
  --region eu-west-2 \
  --callback-urls "http://localhost:3000/callback" "https://localhost:3000/callback" \
  --logout-urls "http://localhost:3000" "https://localhost:3000" \
  --allowed-o-auth-flows code implicit \
  --allowed-o-auth-flows-user-pool-client \
  --allowed-o-auth-scopes email openid profile \
  --explicit-auth-flows ALLOW_USER_SRP_AUTH ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH \
  --generate-secret false
```

### 4. Option A: Make name attribute optional (Recommended)

If you have a custom User Pool, you might not be able to modify the schema. In that case, try this approach by updating our frontend code to handle the missing name gracefully.

### 5. Option B: Use a different approach (If schema can't be modified)

If you can't modify the User Pool schema, we can update our frontend authentication to handle this differently.

## Alternative Frontend Fix

Let me update the authentication context to handle missing name attributes:

### Update the Cognito Auth Context

The issue might also be in how we're requesting the authentication. Let me check your current Cognito domain setup:

```bash
# Check if Google is configured as an identity provider
aws cognito-idp list-identity-providers \
  --user-pool-id eu-west-2_MNEAwllzl \
  --region eu-west-2
```

## If Google Identity Provider is not set up:

You need to set up Google as an identity provider in Cognito. This requires:

1. **Google Cloud Console Setup**:
   - Go to Google Cloud Console
   - Create OAuth 2.0 credentials
   - Add redirect URI: `https://eu-west-2mneawllzl.auth.eu-west-2.amazoncognito.com/oauth2/idpresponse`

2. **Configure Google in Cognito**:
```bash
# Create Google identity provider (replace with your actual Google OAuth credentials)
aws cognito-idp create-identity-provider \
  --user-pool-id eu-west-2_MNEAwllzl \
  --provider-name Google \
  --provider-type Google \
  --region eu-west-2 \
  --provider-details '{
    "client_id": "YOUR_GOOGLE_CLIENT_ID",
    "client_secret": "YOUR_GOOGLE_CLIENT_SECRET",
    "authorize_scopes": "openid email profile"
  }' \
  --attribute-mapping '{
    "email": "email",
    "name": "name",
    "given_name": "given_name",
    "family_name": "family_name"
  }'
```

3. **Update User Pool Client to use Google**:
```bash
aws cognito-idp update-user-pool-client \
  --user-pool-id eu-west-2_MNEAwllzl \
  --client-id 1cn854vio5n33n0rk4f749v6m2 \
  --region eu-west-2 \
  --supported-identity-providers COGNITO Google
```

## Test the Fix

After applying the fixes:

1. Restart your Next.js dev server:
   ```bash
   yarn dev
   ```

2. Visit `http://localhost:3000`
3. Click "Sign In"
4. Try the Google login option

## Quick Debugging

If you're still having issues, check:

1. **Browser Network tab**: Look for the actual redirect URL being used
2. **Cognito User Pool console**: Check the App Integration settings
3. **Google Cloud Console**: Verify the redirect URIs are correct

## Need Google OAuth Setup?

If you haven't set up Google OAuth yet, you need to:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URIs:
   - `https://eu-west-2mneawllzl.auth.eu-west-2.amazoncognito.com/oauth2/idpresponse`
   - `http://localhost:3000/callback` (for testing)

Let me know what you find when you run the diagnostic commands above, and I can provide more specific help!