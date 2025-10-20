/**
 * Simple in-memory cache with TTL support for frontend API responses
 */

interface CacheEntry<T> {
  data: T
  timestamp: number
  ttl: number
}

class Cache {
  private cache = new Map<string, CacheEntry<any>>()
  private pendingRequests = new Map<string, Promise<any>>()

  /**
   * Set a cache entry with TTL in milliseconds
   */
  set<T>(key: string, data: T, ttlMs: number): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl: ttlMs,
    })
  }

  /**
   * Get a cache entry if it exists and hasn't expired
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key)
    
    if (!entry) {
      return null
    }

    const now = Date.now()
    const age = now - entry.timestamp
    
    if (age > entry.ttl) {
      // Entry has expired, remove it
      this.cache.delete(key)
      return null
    }

    return entry.data as T
  }

  /**
   * Remove a specific cache entry
   */
  remove(key: string): void {
    this.cache.delete(key)
    this.pendingRequests.delete(key)
  }

  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear()
    this.pendingRequests.clear()
  }

  /**
   * Remove expired entries (cleanup)
   */
  cleanup(): void {
    const now = Date.now()
    for (const [key, entry] of this.cache.entries()) {
      if (now - entry.timestamp > entry.ttl) {
        this.cache.delete(key)
      }
    }
  }
}

// Singleton cache instance
export const cache = new Cache()

// Cache TTL constants
export const CACHE_TTL = {
  FIVE_MINUTES: 5 * 60 * 1000,
  TEN_MINUTES: 10 * 60 * 1000,
  ONE_HOUR: 60 * 60 * 1000,
} as const

/**
 * Utility function to create cache-aware API calls with request deduplication
 */
export async function cacheableRequest<T>(
  cacheKey: string,
  requestFn: () => Promise<T>,
  ttlMs: number = CACHE_TTL.FIVE_MINUTES
): Promise<T> {
  // Try to get from cache first
  const cached = cache.get<T>(cacheKey)
  if (cached !== null) {
    return cached
  }

  // Check if there's already a pending request for this key
  const pendingRequest = cache.pendingRequests.get(cacheKey)
  if (pendingRequest) {
    return pendingRequest as Promise<T>
  }
  
  // Create the request promise and store it to prevent duplicates
  const requestPromise = requestFn().then((result) => {
    // Cache the result
    cache.set(cacheKey, result, ttlMs)
    // Remove from pending requests
    cache.pendingRequests.delete(cacheKey)
    return result
  }).catch((error) => {
    // Remove from pending requests on error too
    cache.pendingRequests.delete(cacheKey)
    throw error
  })
  
  // Store the pending request
  cache.pendingRequests.set(cacheKey, requestPromise)
  
  return requestPromise
}

// Cleanup expired entries every 10 minutes
if (typeof window !== "undefined") {
  setInterval(() => {
    cache.cleanup()
  }, 10 * 60 * 1000)
}