// Party management service

import { authenticatedApiClient } from "@/lib/authenticated-api-client"
import { cacheableRequest, cache, CACHE_TTL } from "@/lib/cache"
import type { Party, CreatePartyInput, BackendApiResponse } from "@/types"

export const partyService = {
  async createParty(input: CreatePartyInput): Promise<Party> {
    const response = await authenticatedApiClient.post<BackendApiResponse<Party>>("/party", input)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to create party")
    }
    
    // Invalidate parties list cache since we created a new party
    cache.remove('parties:all')
    
    return response.data!
  },

  async getParty(id: string): Promise<Party> {
    return cacheableRequest(
      `party:${id}`,
      async () => {
        const response = await authenticatedApiClient.get<BackendApiResponse<Party>>(`/party/${id}`)
        if (!response.success) {
          throw new Error(response.errorMessage || "Failed to get party")
        }
        return response.data!
      },
      CACHE_TTL.FIVE_MINUTES
    )
  },

  async updateParty(id: string, updates: Partial<Party>): Promise<Party> {
    const response = await authenticatedApiClient.put<BackendApiResponse<Party>>(`/party/${id}`, updates)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to update party")
    }
    
    // Invalidate cache for this specific party and the parties list
    cache.remove(`party:${id}`)
    cache.remove('parties:all')
    
    return response.data!
  },

  async deleteParty(id: string): Promise<void> {
    const response = await authenticatedApiClient.delete<BackendApiResponse<void>>(`/party/${id}`)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to delete party")
    }
    
    // Invalidate cache for this specific party and the parties list
    cache.remove(`party:${id}`)
    cache.remove('parties:all')
  },

  async getParties(): Promise<Party[]> {
    return cacheableRequest(
      'parties:all',
      async () => {
        console.log('#######FETCHING FROM BACKEND#######')
        const response = await authenticatedApiClient.get<BackendApiResponse<Party[]>>("/parties")
        if (!response.success) {
          throw new Error(response.errorMessage || "Failed to get parties")
        }
        // The response.data contains {parties: Party[]}, we need just the array
        const data = response.data!
        console.log('Backend response.data:', data)
        
        // Handle both possible response formats
        if (data && typeof data === 'object' && 'parties' in data) {
          return (data as any).parties as Party[]
        }
        
        // Fallback: if it's already an array, return it
        return Array.isArray(data) ? data : []
      },
      CACHE_TTL.FIVE_MINUTES
    )
  },

  // Note: These endpoints need to be implemented in the backend
  async inviteMember(partyId: string, email: string): Promise<Party> {
    throw new Error("inviteMember endpoint not implemented in backend yet")
    // const response = await apiClient.post<BackendApiResponse<Party>>(`/party/${partyId}/invite`, { email })
    // if (!response.success) {
    //   throw new Error(response.errorMessage || "Failed to invite member")
    // }
    // return response.data!
  },

  async removeMember(partyId: string, userId: string): Promise<Party> {
    throw new Error("removeMember endpoint not implemented in backend yet")
    // const response = await apiClient.delete<BackendApiResponse<Party>>(`/party/${partyId}/members/${userId}`)
    // if (!response.success) {
    //   throw new Error(response.errorMessage || "Failed to remove member")
    // }
    // return response.data!
  },
}
