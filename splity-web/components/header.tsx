"use client"

import Link from "next/link"
import { Moon, Sun, User, LogOut } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useState, useEffect } from "react"
import { useCognitoAuth } from "@/contexts/cognito-auth-context"

export function Header() {
  const [isDark, setIsDark] = useState(false)
  const [isClient, setIsClient] = useState(false)
  const { isAuthenticated, isLoading, user, signIn, signOut } = useCognitoAuth()

  useEffect(() => {
    // Mark as client-side rendering to prevent hydration mismatch
    setIsClient(true)
    
    // Check initial theme only on client side
    if (typeof window !== 'undefined') {
      const isDarkMode = document.documentElement.classList.contains("dark")
      setIsDark(isDarkMode)
    }
  }, [])

  const toggleTheme = () => {
    if (typeof window !== 'undefined') {
      document.documentElement.classList.toggle("dark")
      setIsDark(!isDark)
    }
  }

  return (
    <header className="border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary">
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                className="h-6 w-6 text-primary-foreground"
              >
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                <polyline points="9 22 9 12 15 12 15 22" />
              </svg>
            </div>
            <span className="text-xl font-bold text-foreground">Splity</span>
          </Link>

          {/* Navigation */}
          <nav className="flex items-center gap-6">
            {isClient && isAuthenticated && (
              <>
                <Link
                  href="/dashboard"
                  className="text-sm font-medium text-foreground transition-colors hover:text-primary"
                >
                  Dashboard
                </Link>
                <Link
                  href="/dashboard/analytics"
                  className="text-sm font-medium text-foreground transition-colors hover:text-primary"
                >
                  Analytics
                </Link>
              </>
            )}

            <button
              onClick={toggleTheme}
              className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
              aria-label="Toggle theme"
            >
              {isDark ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            </button>

            {!isClient ? (
              // Show placeholder during SSR to match client initial state
              <div className="h-9 w-20 rounded bg-muted" />
            ) : isLoading ? (
              <div className="h-9 w-20 animate-pulse rounded bg-muted" />
            ) : isAuthenticated ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="icon">
                    <User className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuLabel>
                    {user?.name || user?.email || 'User'}
                  </DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem asChild>
                    <Link href="/dashboard">Dashboard</Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link href="/dashboard/analytics">Analytics</Link>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={signOut}>
                    <LogOut className="mr-2 h-4 w-4" />
                    Sign Out
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : (
              <Button onClick={signIn}>Sign In</Button>
            )}
          </nav>
        </div>
      </div>
    </header>
  )
}
