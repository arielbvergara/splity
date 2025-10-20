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
    console.log(`Cache SET for key: ${key}, TTL: ${ttlMs}ms, cache size before: ${this.cache.size}`)
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl: ttlMs,
    })
    console.log(`Cache size after SET: ${this.cache.size}`)
  }

  /**
   * Get a cache entry if it exists and hasn't expired
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key)
    console.log(`Cache GET for key: ${key}, entry exists: ${!!entry}, cache size: ${this.cache.size}`)
    
    if (!entry) {
      return null
    }

    const now = Date.now()
    const age = now - entry.timestamp
    console.log(`Cache entry age: ${age}ms, TTL: ${entry.ttl}ms, expired: ${age > entry.ttl}`)
    
    if (age > entry.ttl) {
      // Entry has expired, remove it
      console.log(`Cache entry expired for key: ${key}`)
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
    console.log(`Cache HIT for key: ${cacheKey}`, cached)
    return cached
  }

  // Check if there's already a pending request for this key
  const pendingRequest = cache.pendingRequests.get(cacheKey)
  if (pendingRequest) {
    console.log(`Cache PENDING for key: ${cacheKey}, waiting for existing request`)
    return pendingRequest as Promise<T>
  }

  console.log(`Cache MISS for key: ${cacheKey}, making request`)
  
  // Create the request promise and store it to prevent duplicates
  const requestPromise = requestFn().then((result) => {
    console.log(`Caching result for key: ${cacheKey}`, result)
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