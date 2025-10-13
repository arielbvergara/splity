"use client"

import React, { createContext, useContext, useEffect, useState, useCallback } from 'react'
import { AuthProvider as OidcAuthProvider, useAuth as useOidcAuth } from 'react-oidc-context'
import type { User as OidcUser } from 'oidc-client-ts'
import type { User, UserWithDetails, CreateUserInput } from '@/types'
import { userService } from '@/services/user-service'

// Cognito OIDC Configuration
const cognitoAuthConfig = {
  authority: process.env.NEXT_PUBLIC_COGNITO_AUTHORITY!,
  client_id: process.env.NEXT_PUBLIC_COGNITO_CLIENT_ID!,
  redirect_uri: process.env.NEXT_PUBLIC_COGNITO_REDIRECT_URI!,
  response_type: "code",
  scope: "email openid profile",
  automaticSilentRenew: true,
  loadUserInfo: true,
  // Handle cases where name might not be available
  extraQueryParams: {
    // This ensures we request profile information
    response_mode: 'query'
  }
}

interface CognitoAuthContextType {
  // Cognito user data
  isAuthenticated: boolean
  isLoading: boolean
  cognitoUser: OidcUser | null | undefined
  
  // Splity user data
  user: User | null
  userDetails: UserWithDetails | null
  
  // Auth methods
  signIn: () => void
  signOut: () => void
  updateUser: (updates: Partial<User>) => Promise<void>
  
  // Tokens for API calls
  getAccessToken: () => string | null
  getIdToken: () => string | null
}

const CognitoAuthContext = createContext<CognitoAuthContextType | undefined>(undefined)

// Inner component that has access to OIDC context
function CognitoAuthInner({ children }: { children: React.ReactNode }) {
  const oidcAuth = useOidcAuth()
  const [user, setUser] = useState<User | null>(null)
  const [userDetails, setUserDetails] = useState<UserWithDetails | null>(null)
  const [loading, setLoading] = useState(true)

  // Extract Cognito user info
  const cognitoUser = oidcAuth.user
  const isAuthenticated = oidcAuth.isAuthenticated
  const isOidcLoading = oidcAuth.isLoading

  const getAccessToken = useCallback(() => {
    return cognitoUser?.access_token || null
  }, [cognitoUser])

  const getIdToken = useCallback(() => {
    return cognitoUser?.id_token || null
  }, [cognitoUser])

  const signIn = useCallback(() => {
    oidcAuth.signinRedirect()
  }, [oidcAuth])

  const signOut = useCallback(() => {
    const cognitoDomain = process.env.NEXT_PUBLIC_COGNITO_DOMAIN!
    const clientId = process.env.NEXT_PUBLIC_COGNITO_CLIENT_ID!
    const logoutUri = process.env.NEXT_PUBLIC_COGNITO_LOGOUT_URI!
    
    // Clear local user data
    setUser(null)
    setUserDetails(null)
    
    // Sign out from OIDC
    oidcAuth.removeUser()
    
    // Redirect to Cognito logout
    window.location.href = `${cognitoDomain}/logout?client_id=${clientId}&logout_uri=${encodeURIComponent(logoutUri)}`
  }, [oidcAuth])

  // Load or create Splity user when Cognito user changes
  useEffect(() => {
    const loadUser = async () => {
      if (!isAuthenticated || !cognitoUser?.profile) {
        setUser(null)
        setUserDetails(null)
        setLoading(false)
        return
      }

      try {
        const email = cognitoUser.profile.email as string
        const name = (
          cognitoUser.profile.name || 
          cognitoUser.profile.given_name || 
          `${cognitoUser.profile.given_name || ''} ${cognitoUser.profile.family_name || ''}`.trim() ||
          email.split('@')[0]
        ) as string
        const cognitoUserId = cognitoUser.profile.sub as string

        // Try to find existing user by email
        let splityUserDetails: UserWithDetails | null = null
        
        try {
          // First try to get current user (will work if we have a stored userId)
          splityUserDetails = await userService.getCurrentUser()
          
          // If we don't have a stored user, try to find by email or create new
          if (!splityUserDetails) {
            // Try to create or get user - the backend should handle checking if user exists
            const createUserData: CreateUserInput = {
              name,
              email,
              cognitoUserId
            }
            
            const newUser = await userService.createUser(createUserData)
            
            // Store user ID for future requests
            localStorage.setItem("userId", newUser.userId)
            
            // Get full user details
            splityUserDetails = await userService.getUser(newUser.userId)
          }
        } catch (error: any) {
          // If user already exists, try to find them
          if (error.message?.includes("already exists")) {
            // The backend should ideally return the existing user, but for now we'll handle it
            console.warn("User already exists, attempting to find existing user")
          } else {
            console.error("Error creating/finding user:", error)
          }
        }

        if (splityUserDetails) {
          setUserDetails(splityUserDetails)
          setUser(userService.extractUser(splityUserDetails))
          localStorage.setItem("userId", splityUserDetails.user.userId)
        }
      } catch (error) {
        console.error("Failed to load/create user:", error)
      } finally {
        setLoading(false)
      }
    }

    loadUser()
  }, [isAuthenticated, cognitoUser])

  const updateUser = useCallback(async (updates: Partial<User>) => {
    if (!user) return

    try {
      const updatedUser = await userService.updateUser(user.userId, updates)
      setUser(updatedUser)
      // Reload full user details
      const updatedDetails = await userService.getUser(user.userId)
      setUserDetails(updatedDetails)
    } catch (error) {
      console.error("Failed to update user:", error)
      throw error
    }
  }, [user])

  const contextValue: CognitoAuthContextType = {
    isAuthenticated,
    isLoading: isOidcLoading || loading,
    cognitoUser,
    user,
    userDetails,
    signIn,
    signOut,
    updateUser,
    getAccessToken,
    getIdToken,
  }

  return <CognitoAuthContext.Provider value={contextValue}>{children}</CognitoAuthContext.Provider>
}

// Main provider component
export function CognitoAuthProvider({ children }: { children: React.ReactNode }) {
  return (
    <OidcAuthProvider {...cognitoAuthConfig}>
      <CognitoAuthInner>{children}</CognitoAuthInner>
    </OidcAuthProvider>
  )
}

// Hook to use the Cognito auth context
export function useCognitoAuth() {
  const context = useContext(CognitoAuthContext)
  if (context === undefined) {
    throw new Error('useCognitoAuth must be used within a CognitoAuthProvider')
  }
  return context
}

// Export for backward compatibility
export const useAuth = useCognitoAuth