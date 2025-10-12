const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "https://sicb7dcxbd.execute-api.eu-west-2.amazonaws.com/dev"

export class AuthenticatedApiClient {
  private baseUrl: string
  private getAccessToken: (() => string | null) | null = null

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl
  }

  // Set the token getter function (will be called from auth context)
  setTokenGetter(getAccessToken: () => string | null) {
    this.getAccessToken = getAccessToken
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`

    // Get the current access token
    const accessToken = this.getAccessToken?.()

    const config: RequestInit = {
      ...options,
      headers: {
        "Content-Type": "application/json",
        // Add Authorization header if we have a token
        ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
        ...options.headers,
      },
    }

    try {
      const response = await fetch(url, config)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        
        // Handle authentication errors
        if (response.status === 401) {
          // Token might be expired, let the auth context handle this
          console.warn('Authentication failed, token may be expired')
        }
        
        throw new Error(errorData.errorMessage || errorData.message || `HTTP ${response.status}: ${response.statusText}`, {
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
export const authenticatedApiClient = new AuthenticatedApiClient()