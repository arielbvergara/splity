"use client"

import { TrendingUp, TrendingDown, DollarSign, Calendar } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import { formatCurrency, formatPercentage } from "@/lib/utils"

export function SpendingInsights() {
  // Mock data - in real app, this would come from API
  const insights = {
    totalSpent: 3456.78,
    monthlyAverage: 1152.26,
    topCategory: "Food & Dining",
    topCategoryAmount: 1234.5,
    monthlyChange: 12.5,
    isIncreasing: true,
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <Card>
        <CardContent className="p-6">
          <div className="flex items-center gap-3">
            <div className="rounded-lg bg-primary/10 p-2">
              <DollarSign className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1">
              <p className="text-sm text-muted-foreground">Total Spent</p>
              <p className="text-2xl font-bold text-foreground">{formatCurrency(insights.totalSpent)}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-6">
          <div className="flex items-center gap-3">
            <div className="rounded-lg bg-primary/10 p-2">
              <Calendar className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1">
              <p className="text-sm text-muted-foreground">Monthly Average</p>
              <p className="text-2xl font-bold text-foreground">{formatCurrency(insights.monthlyAverage)}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-6">
          <div className="flex items-center gap-3">
            <div className={`rounded-lg p-2 ${insights.isIncreasing ? "bg-destructive/10" : "bg-success/10"}`}>
              {insights.isIncreasing ? (
                <TrendingUp className="h-5 w-5 text-destructive" />
              ) : (
                <TrendingDown className="h-5 w-5 text-success" />
              )}
            </div>
            <div className="flex-1">
              <p className="text-sm text-muted-foreground">Monthly Change</p>
              <p className={`text-2xl font-bold ${insights.isIncreasing ? "text-destructive" : "text-success"}`}>
                {insights.isIncreasing ? "+" : "-"}
                {formatPercentage(Math.abs(insights.monthlyChange), 1)}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-6">
          <div className="flex-1">
            <p className="text-sm text-muted-foreground">Top Category</p>
            <p className="mt-1 text-lg font-bold text-foreground">{insights.topCategory}</p>
            <p className="text-sm text-muted-foreground">{formatCurrency(insights.topCategoryAmount)}</p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
