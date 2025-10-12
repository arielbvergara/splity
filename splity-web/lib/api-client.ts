const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "https://sicb7dcxbd.execute-api.eu-west-2.amazonaws.com/dev"

export class ApiClient {
  private baseUrl: string

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`

    const config: RequestInit = {
      ...options,
      headers: {
        "Content-Type": "application/json",
        ...options.headers,
      },
    }

    try {
      const response = await fetch(url, config)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`, {
          cause: { statusCode: response.status, details: errorData },
        })
      }

      const data = await response.json()
      return data
    } catch (error) {
      if (error instanceof Error && error.cause) {
        throw error
      }

      // Network or parsing errors
      throw new Error(error instanceof Error ? error.message : "An unexpected error occurred", {
        cause: { statusCode: 0, details: error },
      })
    }
  }

  async get<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: "GET" })
  }

  async post<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: "POST",
      body: data ? JSON.stringify(data) : undefined,
    })
  }

  async put<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: "PUT",
      body: data ? JSON.stringify(data) : undefined,
    })
  }

  async delete<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: "DELETE" })
  }
}

// Singleton instance
export const apiClient = new ApiClient()
