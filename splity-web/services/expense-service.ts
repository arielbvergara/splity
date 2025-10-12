// Expense management service

import { apiClient } from "@/lib/api-client"
import type { Expense, CreateExpenseInput, BackendApiResponse, ReceiptData } from "@/types"

export const expenseService = {
  async createExpense(input: CreateExpenseInput): Promise<Expense> {
    const response = await apiClient.post<BackendApiResponse<Expense>>("/expenses", input)
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to create expense")
    }
    return response.data!
  },

  async deleteExpense(expenseData: { expenseId: string; partyId: string }): Promise<void> {
    // Backend expects expenseId and partyId in request body for DELETE
    const response = await apiClient.delete<BackendApiResponse<void>>("/expenses", {
      body: JSON.stringify(expenseData),
    })
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to delete expense")
    }
  },

  // Note: Update expense endpoint not implemented in backend
  async updateExpense(id: string, updates: Partial<Expense>): Promise<Expense> {
    throw new Error("updateExpense endpoint not implemented in backend yet")
    // const response = await apiClient.put<BackendApiResponse<Expense>>(`/expenses/${id}`, updates)
    // if (!response.success) {
    //   throw new Error(response.errorMessage || "Failed to update expense")
    // }
    // return response.data!
  },

  async uploadReceipt(partyId: string, file: File): Promise<ReceiptData> {
    const formData = new FormData()
    formData.append("receipt", file)

    const response = await apiClient.put<BackendApiResponse<ReceiptData>>(`/party/${partyId}/extract`, formData, {
      headers: {} as HeadersInit, // Let browser set Content-Type for FormData
    })
    if (!response.success) {
      throw new Error(response.errorMessage || "Failed to upload receipt")
    }
    return response.data!
  },

  // Note: Get expenses by party endpoint not implemented in backend
  async getExpenses(partyId: string): Promise<Expense[]> {
    throw new Error("getExpenses endpoint not implemented in backend yet")
    // This would need to be added to the backend or retrieved via party details
    // const response = await apiClient.get<BackendApiResponse<Expense[]>>(`/party/${partyId}/expenses`)
    // if (!response.success) {
    //   throw new Error(response.errorMessage || "Failed to get expenses")
    // }
    // return response.data!
  },
}
