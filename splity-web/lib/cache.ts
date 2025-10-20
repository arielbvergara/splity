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
  }

  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear()
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
 * Utility function to create cache-aware API calls
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

  console.log(`Cache MISS for key: ${cacheKey}, making request`)
  // Not in cache, make the request
  const result = await requestFn()
  
  console.log(`Caching result for key: ${cacheKey}`, result)
  // Cache the result
  cache.set(cacheKey, result, ttlMs)
  
  return result
}

// Cleanup expired entries every 10 minutes
if (typeof window !== "undefined") {
  setInterval(() => {
    cache.cleanup()
  }, 10 * 60 * 1000)
}