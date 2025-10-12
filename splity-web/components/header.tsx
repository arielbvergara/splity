"use client"

import Link from "next/link"
import { Moon, Sun } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useState, useEffect } from "react"

export function Header() {
  const [isDark, setIsDark] = useState(false)

  useEffect(() => {
    // Check initial theme
    const isDarkMode = document.documentElement.classList.contains("dark")
    setIsDark(isDarkMode)
  }, [])

  const toggleTheme = () => {
    document.documentElement.classList.toggle("dark")
    setIsDark(!isDark)
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

            <button
              onClick={toggleTheme}
              className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
              aria-label="Toggle theme"
            >
              {isDark ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            </button>

            <Button>Sign In</Button>
          </nav>
        </div>
      </div>
    </header>
  )
}
