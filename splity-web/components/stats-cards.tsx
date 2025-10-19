import { DollarSign, Users, TrendingUp, Receipt } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"

const stats = [
  {
    title: "Total Expenses",
    value: "$2,534.60",
    subtitle: "This month",
    change: "+12%",
    icon: DollarSign,
    iconBg: "bg-primary/10",
    iconColor: "text-primary",
  },
  {
    title: "Active Parties",
    value: "3",
    subtitle: "2 need settlement",
    icon: Users,
    iconBg: "bg-primary/10",
    iconColor: "text-primary",
  },
  {
    title: "You're Owed",
    value: "$125.25",
    subtitle: "From 1 party",
    change: "+8%",
    icon: TrendingUp,
    iconBg: "bg-success/10",
    iconColor: "text-success",
  },
  {
    title: "Total Receipts",
    value: "24",
    subtitle: "This month",
    icon: Receipt,
    iconBg: "bg-primary/10",
    iconColor: "text-primary",
  },
]

export function StatsCards() {
  return (
    <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {stats.map((stat) => (
        <Card key={stat.title} className="border-border bg-card">
          <CardContent className="p-6">
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <p className="text-sm font-medium text-muted-foreground">{stat.title}</p>
                <p className="mt-2 text-3xl font-bold tracking-tight text-card-foreground">{stat.value}</p>
                <p className="mt-1 text-sm text-muted-foreground">{stat.subtitle}</p>
                {stat.change && <p className="mt-2 text-sm font-medium text-success">↑ {stat.change}</p>}
              </div>
              <div className={`rounded-lg p-3 ${stat.iconBg}`}>
                <stat.icon className={`h-6 w-6 ${stat.iconColor}`} />
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
