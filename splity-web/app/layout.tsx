import type React from "react"
import type { Metadata } from "next"
import { GeistSans } from "geist/font/sans"
import { GeistMono } from "geist/font/mono"
import { Analytics } from "@vercel/analytics/next"
import { Suspense } from "react"
import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"
import { AuthProvider } from "@/contexts/auth-context"
import { Toaster } from "@/components/ui/toaster"
import { PWAInstallPrompt } from "@/components/pwa-install-prompt"
import { OfflineIndicator } from "@/components/offline-indicator"

export const metadata: Metadata = {
  title: "Splity - Split Expenses with Friends",
  description: "Simplify expense sharing and receipt scanning through automation and AI",
  generator: "v0.app",
  manifest: "/manifest.json",
  themeColor: "#6366f1",
  appleWebApp: {
    capable: true,
    statusBarStyle: "default",
    title: "Splity",
  },
  formatDetection: {
    telephone: false,
  },
  openGraph: {
    type: "website",
    siteName: "Splity",
    title: "Splity - Split Expenses with Friends",
    description: "Simplify expense sharing and receipt scanning through automation and AI",
  },
  twitter: {
    card: "summary_large_image",
    title: "Splity - Split Expenses with Friends",
    description: "Simplify expense sharing and receipt scanning through automation and AI",
  },
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <link rel="icon" href="/favicon.ico" sizes="any" />
        <link rel="apple-touch-icon" href="/icon-192.png" />
      </head>
      <body className={`font-sans ${GeistSans.variable} ${GeistMono.variable} antialiased`}>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
          <AuthProvider>
            <Suspense fallback={null}>{children}</Suspense>
            <Toaster />
            <PWAInstallPrompt />
            <OfflineIndicator />
          </AuthProvider>
        </ThemeProvider>
        <Analytics />
      </body>
    </html>
  )
}
