const CACHE_NAME = "splity-v1"
const RUNTIME_CACHE = "splity-runtime"

// Assets to cache on install
const PRECACHE_ASSETS = ["/", "/dashboard", "/dashboard/analytics", "/manifest.json"]

// Install event - cache essential assets
self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(PRECACHE_ASSETS)
    }),
  )
  self.skipWaiting()
})

// Activate event - clean up old caches
self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.filter((name) => name !== CACHE_NAME && name !== RUNTIME_CACHE).map((name) => caches.delete(name)),
      )
    }),
  )
  self.clients.claim()
})

// Fetch event - network first, fallback to cache
self.addEventListener("fetch", (event) => {
  const { request } = event

  // Skip non-GET requests
  if (request.method !== "GET") {
    return
  }

  // Skip API requests for offline handling
  if (request.url.includes("/api/")) {
    event.respondWith(
      fetch(request).catch(() => {
        return new Response(JSON.stringify({ error: "Offline", message: "You are currently offline" }), {
          headers: { "Content-Type": "application/json" },
          status: 503,
        })
      }),
    )
    return
  }

  // Network first, fallback to cache strategy
  event.respondWith(
    fetch(request)
      .then((response) => {
        // Clone the response
        const responseClone = response.clone()

        // Cache the fetched response
        caches.open(RUNTIME_CACHE).then((cache) => {
          cache.put(request, responseClone)
        })

        return response
      })
      .catch(() => {
        // Network failed, try cache
        return caches.match(request).then((cachedResponse) => {
          if (cachedResponse) {
            return cachedResponse
          }

          // Return offline page for navigation requests
          if (request.mode === "navigate") {
            return caches.match("/")
          }

          return new Response("Offline", { status: 503 })
        })
      }),
  )
})

// Background sync for offline actions
self.addEventListener("sync", (event) => {
  if (event.tag === "sync-expenses") {
    event.waitUntil(syncExpenses())
  }
})

async function syncExpenses() {
  // Get pending expenses from IndexedDB
  // Send to server when online
  console.log("[SW] Syncing expenses...")
}
