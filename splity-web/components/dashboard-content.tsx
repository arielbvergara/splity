"use client"

import { StatsCards } from "@/components/stats-cards"
import { PartyList } from "@/components/party-list"
import { useCognitoAuth } from "@/contexts/cognito-auth-context"
import { useRouter } from "next/navigation"
import { useEffect, useState } from "react"

export function DashboardContent() {
  const { isAuthenticated, isLoading } = useCognitoAuth()
  const router = useRouter()
  const [isClient, setIsClient] = useState(false)

  useEffect(() => {
    setIsClient(true)
  }, [])

  useEffect(() => {
    if (isClient && !isLoading && !isAuthenticated) {
      router.push('/')
    }
  }, [isClient, isAuthenticated, isLoading, router])

  // Show consistent loading state during SSR and initial client render
  if (!isClient || isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-lg text-muted-foreground">
            {!isClient ? "Loading..." : "Loading your dashboard..."}
          </p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="text-center">
          <p className="text-lg text-muted-foreground">Redirecting to sign in...</p>
        </div>
      </div>
    )
  }

  return (
    <>
      {/* Stats Cards */}
      <StatsCards />

      {/* Party List */}
      <PartyList />
    </>
  )
}