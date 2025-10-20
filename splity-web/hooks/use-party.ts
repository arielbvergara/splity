"use client"

import { useState, useEffect } from "react"
import type { Party } from "@/types"
import { partyService } from "@/services/party-service"
import { toast } from "@/hooks/use-toast"

export function useParty(partyId: string | null) {
  const [party, setParty] = useState<Party | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<Error | null>(null)

  useEffect(() => {
    if (partyId) {
      loadParty(partyId)
    }
  }, [partyId])

  const loadParty = async (id: string) => {
    try {
      setLoading(true)
      const data = await partyService.getParty(id)
      setParty(data)
      setError(null)
    } catch (err) {
      const error = err instanceof Error ? err : new Error("Failed to load party")
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

  const updateParty = async (updates: Partial<Party>) => {
    if (!party) return

    try {
      const updated = await partyService.updateParty(party.partyId, updates)
      setParty(updated)
      toast({
        title: "Success",
        description: "Party updated successfully",
      })
    } catch (err) {
      const error = err instanceof Error ? err : new Error("Failed to update party")
      toast({
        title: "Error",
        description: error.message,
        variant: "destructive",
      })
      throw error
    }
  }

  return {
    party,
    loading,
    error,
    loadParty,
    updateParty,
  }
}
