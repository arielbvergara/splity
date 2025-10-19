"use client"

import { useState, useEffect } from "react"
import { WifiOff, Wifi } from "lucide-react"
import { Alert, AlertDescription } from "@/components/ui/alert"

export function OfflineIndicator() {
  const [isOnline, setIsOnline] = useState(true)
  const [showAlert, setShowAlert] = useState(false)

  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true)
      setShowAlert(true)
      setTimeout(() => setShowAlert(false), 3000)
    }

    const handleOffline = () => {
      setIsOnline(false)
      setShowAlert(true)
    }

    setIsOnline(navigator.onLine)

    window.addEventListener("online", handleOnline)
    window.addEventListener("offline", handleOffline)

    return () => {
      window.removeEventListener("online", handleOnline)
      window.removeEventListener("offline", handleOffline)
    }
  }, [])

  if (!showAlert) return null

  return (
    <div className="fixed bottom-4 left-1/2 z-50 -translate-x-1/2">
      <Alert className={isOnline ? "border-success bg-success/10" : "border-destructive bg-destructive/10"}>
        <div className="flex items-center gap-2">
          {isOnline ? <Wifi className="h-4 w-4 text-success" /> : <WifiOff className="h-4 w-4 text-destructive" />}
          <AlertDescription className={isOnline ? "text-success" : "text-destructive"}>
            {isOnline ? "You're back online" : "You're offline - some features may be limited"}
          </AlertDescription>
        </div>
      </Alert>
    </div>
  )
}
