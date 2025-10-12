// User management service

import { authenticatedApiClient } from "@/lib/authenticated-api-client"
import type { User, UserWithDetails, UpdateUserInput, CreateUserInput, BackendApiResponse } from "@/types"

export const userService = {
  async createUser(data: CreateUserInput): Promise<User> {
    const response = await authenticatedApiClient.post<BackendApiResponse<User>>("/users", data)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to create user")
    }
    return response.data!
  },

  async getUser(id: string): Promise<UserWithDetails> {
    const response = await authenticatedApiClient.get<BackendApiResponse<UserWithDetails>>(`/users/${id}`)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to get user")
    }
    return response.data!
  },

  async updateUser(id: string, updates: UpdateUserInput): Promise<User> {
    const response = await authenticatedApiClient.put<BackendApiResponse<User>>(`/users/${id}`, updates)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to update user")
    }
    return response.data!
  },

  async getCurrentUser(): Promise<UserWithDetails | null> {
    try {
      // Check localStorage or cookies for user session
      const userId = localStorage.getItem("userId")
      if (!userId) return null
      return await this.getUser(userId)
    } catch (error) {
      console.error("Failed to get current user:", error)
      return null
    }
  },

  // Helper to extract simple user from UserWithDetails
  extractUser(userWithDetails: UserWithDetails): User {
    return {
      userId: userWithDetails.user.userId,
      name: userWithDetails.user.name,
      email: userWithDetails.user.email,
      createdAt: userWithDetails.user.createdAt
    }
  },
}
