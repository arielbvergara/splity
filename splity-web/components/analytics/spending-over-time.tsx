"use client"

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"
import { LineChart, Line, XAxis, YAxis, CartesianGrid, ResponsiveContainer } from "recharts"

export function SpendingOverTime() {
  // Mock data - in real app, this would come from API
  const data = [
    { month: "Jan", amount: 1200 },
    { month: "Feb", amount: 1450 },
    { month: "Mar", amount: 980 },
    { month: "Apr", amount: 1650 },
    { month: "May", amount: 1320 },
    { month: "Jun", amount: 1890 },
    { month: "Jul", amount: 2100 },
    { month: "Aug", amount: 1750 },
    { month: "Sep", amount: 1580 },
    { month: "Oct", amount: 1920 },
    { month: "Nov", amount: 2250 },
    { month: "Dec", amount: 2450 },
  ]

  return (
    <Card>
      <CardHeader>
        <CardTitle>Spending Over Time</CardTitle>
        <CardDescription>Monthly spending trends for the past year</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="h-[400px]">
          <ChartContainer
            config={{
              amount: {
                label: "Amount",
                color: "hsl(var(--primary))",
              },
            }}
          >
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={data}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                <XAxis dataKey="month" className="text-xs" />
                <YAxis className="text-xs" />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Line
                  type="monotone"
                  dataKey="amount"
                  stroke="hsl(var(--primary))"
                  strokeWidth={2}
                  dot={{ fill: "hsl(var(--primary))" }}
                />
              </LineChart>
            </ResponsiveContainer>
          </ChartContainer>
        </div>
      </CardContent>
    </Card>
  )
}
