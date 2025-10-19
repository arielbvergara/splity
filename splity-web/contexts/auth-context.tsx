"use client"

import type React from "react"
import { createContext, useContext, useEffect, useState } from "react"
import type { User, UserWithDetails } from "@/types"
import { userService } from "@/services/user-service"

interface AuthContextType {
  user: User | null
  userDetails: UserWithDetails | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  updateUser: (updates: Partial<User>) => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [userDetails, setUserDetails] = useState<UserWithDetails | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Load user from localStorage on mount
    loadUser()
  }, [])

  const loadUser = async () => {
    try {
      const currentUserDetails = await userService.getCurrentUser()
      if (currentUserDetails) {
        setUserDetails(currentUserDetails)
        setUser(userService.extractUser(currentUserDetails))
      }
    } catch (error) {
      console.error("Failed to load user:", error)
    } finally {
      setLoading(false)
    }
  }

  const login = async (email: string, password: string) => {
    // TODO: Implement actual authentication
    // For now, create a user and store it
    try {
      // Create user if not exists (simplified auth)
      const newUser = await userService.createUser({
        name: email.split('@')[0], // Use email prefix as name
        email
      })
      
      localStorage.setItem("userId", newUser.userId)
      setUser(newUser)
      
      // Reload to get full user details
      await loadUser()
    } catch (error) {
      console.error("Login failed:", error)
      throw error
    }
  }

  const logout = () => {
    localStorage.removeItem("userId")
    setUser(null)
  }

  const updateUser = async (updates: Partial<User>) => {
    if (!user) return

    const updatedUser = await userService.updateUser(user.userId, updates)
    setUser(updatedUser)
    // Reload to get updated details
    await loadUser()
  }

  return <AuthContext.Provider value={{ user, userDetails, loading, login, logout, updateUser }}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider")
  }
  return context
}
