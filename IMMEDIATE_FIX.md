# Immediate Fix for Google Login Error

## The Problem
The error `attributes required: [name]` occurs because:
1. Your redirect URI was pointing to `/` instead of `/callback` 
2. Your Cognito User Pool Client might not be properly configured for the callback URL

## Step 1: Fix Callback URLs (MOST IMPORTANT)

Run this command right now to fix your callback URLs:

```bash
aws cognito-idp update-user-pool-client \
  --user-pool-id eu-west-2_MNEAwllzl \
  --client-id 1cn854vio5n33n0rk4f749v6m2 \
  --region eu-west-2 \
  --callback-urls "http://localhost:3000/callback" \
  --logout-urls "http://localhost:3000" \
  --allowed-o-auth-flows code \
  --allowed-o-auth-flows-user-pool-client \
  --allowed-o-auth-scopes email openid profile \
  --explicit-auth-flows ALLOW_USER_SRP_AUTH ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH
```

## Step 2: Restart Your Dev Server

```bash
# Stop your current dev server (Ctrl+C)
# Then restart it
yarn dev
```

## Step 3: Test Again

1. Go to `http://localhost:3000`
2. Click "Sign In"
3. Try Google login again

## If Still Having Issues

Check if Google is configured as an identity provider:

```bash
aws cognito-idp list-identity-providers \
  --user-pool-id eu-west-2_MNEAwllzl \
  --region eu-west-2
```

If Google is not in the list, you need to set it up first in the Cognito console or follow the full setup guide in GOOGLE_LOGIN_FIX.md.

## Quick Test
After running the commands above, the Google login should work. If you still get an error, let me know what the new error message is!