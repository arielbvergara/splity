"use client"

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from 'react-oidc-context'

export default function CallbackPage() {
  const auth = useAuth()
  const router = useRouter()

  useEffect(() => {
    // Handle the callback
    if (auth.isAuthenticated) {
      // Redirect to dashboard after successful authentication
      router.push('/dashboard')
    } else if (auth.error) {
      // Handle authentication error
      console.error('Authentication error:', auth.error)
      router.push('/?error=auth_failed')
    }
  }, [auth.isAuthenticated, auth.error, router])

  if (auth.isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-lg">Signing you in...</p>
        </div>
      </div>
    )
  }

  if (auth.error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-red-600 mb-4">Authentication Failed</h2>
          <p className="text-gray-600 mb-4">{auth.error.message}</p>
          <button 
            onClick={() => router.push('/')}
            className="bg-primary text-white px-4 py-2 rounded hover:bg-primary/90"
          >
            Return to Home
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p className="text-lg">Redirecting...</p>
      </div>
    </div>
  )
}