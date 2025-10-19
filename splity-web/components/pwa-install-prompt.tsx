"use client"

import { useState, useEffect } from "react"
import { Download, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"

export function PWAInstallPrompt() {
  const [deferredPrompt, setDeferredPrompt] = useState<any>(null)
  const [showPrompt, setShowPrompt] = useState(false)

  useEffect(() => {
    const handler = (e: Event) => {
      e.preventDefault()
      setDeferredPrompt(e)
      setShowPrompt(true)
    }

    window.addEventListener("beforeinstallprompt", handler)

    return () => {
      window.removeEventListener("beforeinstallprompt", handler)
    }
  }, [])

  const handleInstall = async () => {
    if (!deferredPrompt) return

    deferredPrompt.prompt()
    const { outcome } = await deferredPrompt.userChoice

    console.log("[v0] PWA install outcome:", outcome)
    setDeferredPrompt(null)
    setShowPrompt(false)
  }

  const handleDismiss = () => {
    setShowPrompt(false)
    localStorage.setItem("pwa-prompt-dismissed", "true")
  }

  if (!showPrompt) return null

  return (
    <div className="fixed bottom-4 left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-96">
      <Card className="border-primary shadow-lg">
        <CardContent className="p-4">
          <div className="flex items-start gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary">
              <Download className="h-5 w-5 text-primary-foreground" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-foreground">Install Splity</h3>
              <p className="mt-1 text-sm text-muted-foreground">Install our app for quick access and offline support</p>
              <div className="mt-3 flex gap-2">
                <Button size="sm" onClick={handleInstall}>
                  Install
                </Button>
                <Button size="sm" variant="ghost" onClick={handleDismiss}>
                  Not now
                </Button>
              </div>
            </div>
            <Button variant="ghost" size="icon" className="h-8 w-8" onClick={handleDismiss}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
