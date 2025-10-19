"use client"

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"
import { PieChart, Pie, Cell, ResponsiveContainer } from "recharts"
import { formatCurrency } from "@/lib/utils"

export function SpendingByCategory() {
  // Mock data - in real app, this would come from API
  const data = [
    { name: "Food & Dining", value: 1234.5, color: "hsl(var(--chart-1))" },
    { name: "Transportation", value: 567.8, color: "hsl(var(--chart-2))" },
    { name: "Accommodation", value: 890.0, color: "hsl(var(--chart-3))" },
    { name: "Entertainment", value: 345.6, color: "hsl(var(--chart-4))" },
    { name: "Shopping", value: 234.9, color: "hsl(var(--chart-5))" },
    { name: "Other", value: 184.0, color: "hsl(var(--chart-6))" },
  ]

  const total = data.reduce((sum, item) => sum + item.value, 0)

  return (
    <Card>
      <CardHeader>
        <CardTitle>Spending by Category</CardTitle>
        <CardDescription>Breakdown of expenses across different categories</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-6 lg:grid-cols-2">
          {/* Chart */}
          <div className="h-[300px]">
            <ChartContainer
              config={{
                value: {
                  label: "Amount",
                },
              }}
            >
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={data} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={100} label>
                    {data.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <ChartTooltip content={<ChartTooltipContent />} />
                </PieChart>
              </ResponsiveContainer>
            </ChartContainer>
          </div>

          {/* Legend with percentages */}
          <div className="space-y-3">
            {data.map((item) => {
              const percentage = (item.value / total) * 100
              return (
                <div key={item.name} className="flex items-center justify-between rounded-lg border p-3">
                  <div className="flex items-center gap-3">
                    <div className="h-4 w-4 rounded" style={{ backgroundColor: item.color }} />
                    <span className="font-medium text-foreground">{item.name}</span>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-foreground">{formatCurrency(item.value)}</p>
                    <p className="text-xs text-muted-foreground">{percentage.toFixed(1)}%</p>
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
