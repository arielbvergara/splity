"use client"

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, ResponsiveContainer } from "recharts"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { getInitials, getAvatarColor, formatCurrency } from "@/lib/utils"

export function SpendingByMember() {
  // Mock data - in real app, this would come from API
  const data = [
    { name: "Alex Johnson", amount: 1456.78, paid: 1234.5, owed: 222.28 },
    { name: "Sarah Smith", amount: 1234.5, paid: 1456.78, owed: -222.28 },
    { name: "Mike Brown", amount: 987.65, paid: 890.0, owed: 97.65 },
    { name: "Emma Davis", amount: 778.13, paid: 876.5, owed: -98.37 },
  ]

  return (
    <Card>
      <CardHeader>
        <CardTitle>Spending by Member</CardTitle>
        <CardDescription>Individual spending and settlement status</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-6">
          {/* Chart */}
          <div className="h-[300px]">
            <ChartContainer
              config={{
                amount: {
                  label: "Total Spent",
                  color: "hsl(var(--chart-1))",
                },
                paid: {
                  label: "Amount Paid",
                  color: "hsl(var(--chart-2))",
                },
              }}
            >
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="name" className="text-xs" />
                  <YAxis className="text-xs" />
                  <ChartTooltip content={<ChartTooltipContent />} />
                  <Bar dataKey="amount" fill="hsl(var(--chart-1))" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="paid" fill="hsl(var(--chart-2))" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </ChartContainer>
          </div>

          {/* Member Details */}
          <div className="space-y-3">
            {data.map((member) => (
              <div key={member.name} className="flex items-center justify-between rounded-lg border p-4">
                <div className="flex items-center gap-3">
                  <Avatar className="h-10 w-10">
                    <AvatarFallback className={`${getAvatarColor(member.name)} text-white`}>
                      {getInitials(member.name)}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <p className="font-semibold text-foreground">{member.name}</p>
                    <p className="text-sm text-muted-foreground">
                      Spent {formatCurrency(member.amount)} • Paid {formatCurrency(member.paid)}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p
                    className={`text-lg font-bold ${member.owed > 0 ? "text-success" : member.owed < 0 ? "text-destructive" : "text-muted-foreground"}`}
                  >
                    {member.owed > 0 ? "+" : ""}
                    {formatCurrency(member.owed)}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {member.owed > 0 ? "Owed" : member.owed < 0 ? "Owes" : "Settled"}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
