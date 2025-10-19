"use client"

import { useState, useEffect } from "react"
import type { Party, CreatePartyInput } from "@/types"
import { partyService } from "@/services/party-service"
import { toast } from "@/hooks/use-toast"
import { useCognitoAuth } from "@/contexts/cognito-auth-context"

export function useParties() {
  const [parties, setParties] = useState<Party[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<Error | null>(null)
  const { user, userDetails } = useCognitoAuth()

  useEffect(() => {
    // Load parties from user details instead of separate endpoint (not implemented)
    if (userDetails) {
      setParties(userDetails.user.ownedParties || [])
      setLoading(false)
      setError(null)
    } else {
      // loadParties() // Uncomment when backend implements getParties endpoint
      setLoading(false)
    }
  }, [userDetails])

  const loadParties = async () => {
    try {
      setLoading(true)
      // TODO: Uncomment when backend implements getParties endpoint
      // const data = await partyService.getParties()
      // setParties(data)
      setError(null)
      toast({
        title: "Info",
        description: "Party list loading from user details - getParties endpoint not implemented yet",
      })
    } catch (err) {
      const error = err instanceof Error ? err : new Error("Failed to load parties")
      setError(error)
      toast({
        title: "Error",
        description: error.message,
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const createParty = async (input: { name: string; description?: string }) => {
    if (!user) {
      const error = new Error("User must be logged in to create party")
      toast({
        title: "Error",
        description: error.message,
        variant: "destructive",
      })
      throw error
    }
    
    try {
      const partyInput: CreatePartyInput = {
        ownerId: user.userId,
        name: input.name,
        description: input.description
      }
      const newParty = await partyService.createParty(partyInput)
      setParties((prev) => [newParty, ...prev])
      toast({
        title: "Success",
        description: "Party created successfully",
      })
      return newParty
    } catch (err) {
      const error = err instanceof Error ? err : new Error("Failed to create party")
      toast({
        title: "Error",
        description: error.message,
        variant: "destructive",
      })
      throw error
    }
  }

  const deleteParty = async (partyId: string) => {
    try {
      await partyService.deleteParty(partyId)
      setParties((prev) => prev.filter((p) => p.partyId !== partyId))
      toast({
        title: "Success",
        description: "Party deleted successfully",
      })
    } catch (err) {
      const error = err instanceof Error ? err : new Error("Failed to delete party")
      toast({
        title: "Error",
        description: error.message,
        variant: "destructive",
      })
      throw error
    }
  }

  return {
    parties,
    loading,
    error,
    loadParties,
    createParty,
    deleteParty,
  }
}
