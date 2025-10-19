// PWA service worker registration

export function registerServiceWorker() {
  if (typeof window !== "undefined" && "serviceWorker" in navigator) {
    window.addEventListener("load", () => {
      navigator.serviceWorker
        .register("/service-worker.js")
        .then((registration) => {
          console.log("[v0] Service Worker registered:", registration)

          // Check for updates
          registration.addEventListener("updatefound", () => {
            const newWorker = registration.installing
            if (newWorker) {
              newWorker.addEventListener("statechange", () => {
                if (newWorker.state === "installed" && navigator.serviceWorker.controller) {
                  console.log("[v0] New service worker available")
                  // Optionally show update notification
                }
              })
            }
          })
        })
        .catch((error) => {
          console.error("[v0] Service Worker registration failed:", error)
        })
    })
  }
}

// Call this in your app initialization
if (typeof window !== "undefined") {
  registerServiceWorker()
}
