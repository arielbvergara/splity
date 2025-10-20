/**
 * Simple tests for cache functionality
 */

import { cache, cacheableRequest, CACHE_TTL } from '../cache'

describe('Cache', () => {
  beforeEach(() => {
    cache.clear()
  })

  test('should cache and retrieve data', () => {
    const testData = { id: 1, name: 'test' }
    cache.set('test-key', testData, 5000)
    
    const retrieved = cache.get('test-key')
    expect(retrieved).toEqual(testData)
  })

  test('should return null for non-existent keys', () => {
    const retrieved = cache.get('non-existent')
    expect(retrieved).toBeNull()
  })

  test('should expire data after TTL', (done) => {
    const testData = { id: 1, name: 'test' }
    cache.set('test-key', testData, 100) // 100ms TTL
    
    // Should be available immediately
    expect(cache.get('test-key')).toEqual(testData)
    
    // Should expire after TTL
    setTimeout(() => {
      expect(cache.get('test-key')).toBeNull()
      done()
    }, 150)
  })

  test('cacheableRequest should cache function results', async () => {
    let callCount = 0
    const testFunction = () => {
      callCount++
      return Promise.resolve({ data: `call-${callCount}` })
    }

    // First call should execute function
    const result1 = await cacheableRequest('test-fn', testFunction, 5000)
    expect(result1).toEqual({ data: 'call-1' })
    expect(callCount).toBe(1)

    // Second call should return cached result
    const result2 = await cacheableRequest('test-fn', testFunction, 5000)
    expect(result2).toEqual({ data: 'call-1' })
    expect(callCount).toBe(1) // Function should not be called again
  })

  test('should remove specific cache entries', () => {
    cache.set('key1', 'value1', 5000)
    cache.set('key2', 'value2', 5000)
    
    cache.remove('key1')
    
    expect(cache.get('key1')).toBeNull()
    expect(cache.get('key2')).toBe('value2')
  })

  test('should clear all cache entries', () => {
    cache.set('key1', 'value1', 5000)
    cache.set('key2', 'value2', 5000)
    
    cache.clear()
    
    expect(cache.get('key1')).toBeNull()
    expect(cache.get('key2')).toBeNull()
  })
})