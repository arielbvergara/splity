"use client"

import { useEffect } from 'react'
import { useCognitoAuth } from '@/contexts/cognito-auth-context'
import { authenticatedApiClient } from '@/lib/authenticated-api-client'

export function AuthInitializer() {
  const { getAccessToken } = useCognitoAuth()

  useEffect(() => {
    // Set up the authenticated API client with the token getter
    authenticatedApiClient.setTokenGetter(getAccessToken)
  }, [getAccessToken])

  // This component doesn't render anything
  return null
}