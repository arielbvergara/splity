"use client"

import { Header } from "@/components/header"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { SpendingByCategory } from "@/components/analytics/spending-by-category"
import { SpendingOverTime } from "@/components/analytics/spending-over-time"
import { SpendingByMember } from "@/components/analytics/spending-by-member"
import { SpendingInsights } from "@/components/analytics/spending-insights"
import { Button } from "@/components/ui/button"
import { Download } from "lucide-react"

export default function AnalyticsPage() {
  return (
    <div className="min-h-screen bg-background">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-4xl font-bold tracking-tight text-foreground">Analytics</h1>
            <p className="mt-2 text-lg text-muted-foreground">Track your spending patterns and insights</p>
          </div>
          <Button variant="outline" className="gap-2 bg-transparent">
            <Download className="h-4 w-4" />
            Export Report
          </Button>
        </div>

        {/* Insights Cards */}
        <SpendingInsights />

        {/* Charts */}
        <Tabs defaultValue="category" className="mt-8 space-y-6">
          <TabsList>
            <TabsTrigger value="category">By Category</TabsTrigger>
            <TabsTrigger value="time">Over Time</TabsTrigger>
            <TabsTrigger value="member">By Member</TabsTrigger>
          </TabsList>

          <TabsContent value="category">
            <SpendingByCategory />
          </TabsContent>

          <TabsContent value="time">
            <SpendingOverTime />
          </TabsContent>

          <TabsContent value="member">
            <SpendingByMember />
          </TabsContent>
        </Tabs>
      </main>
    </div>
  )
}
