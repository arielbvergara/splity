// Party management service

import { apiClient } from "@/lib/api-client"
import type { Party, CreatePartyInput, BackendApiResponse } from "@/types"

export const partyService = {
  async createParty(input: CreatePartyInput): Promise<Party> {
    const response = await apiClient.post<BackendApiResponse<Party>>("/party", input)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to create party")
    }
    return response.data!
  },

  async getParty(id: string): Promise<Party> {
    const response = await apiClient.get<BackendApiResponse<Party>>(`/party/${id}`)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to get party")
    }
    return response.data!
  },

  async updateParty(id: string, updates: Partial<Party>): Promise<Party> {
    const response = await apiClient.put<BackendApiResponse<Party>>(`/party/${id}`, updates)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to update party")
    }
    return response.data!
  },

  async deleteParty(id: string): Promise<void> {
    const response = await apiClient.delete<BackendApiResponse<void>>(`/party/${id}`)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to delete party")
    }
  },

  // Note: These endpoints need to be implemented in the backend
  async getParties(): Promise<Party[]> {
    throw new Error("getParties endpoint not implemented in backend yet")
    // const response = await apiClient.get<BackendApiResponse<Party[]>>("/parties")
    // if (!response.success) {
    //   throw new Error(response.errorMessage || "Failed to get parties")
    // }
    // return response.data!
  },

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
