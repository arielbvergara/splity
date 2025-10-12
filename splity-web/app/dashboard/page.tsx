import { Header } from "@/components/header"
import { StatsCards } from "@/components/stats-cards"
import { PartyList } from "@/components/party-list"

export default function DashboardPage() {
  return (
    <div className="min-h-screen bg-background">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {/* Dashboard Header */}
        <div className="mb-8">
          <h1 className="text-4xl font-bold tracking-tight text-foreground">Dashboard</h1>
          <p className="mt-2 text-lg text-muted-foreground">Manage your expenses and settlements</p>
        </div>

        {/* Stats Cards */}
        <StatsCards />

        {/* Party List */}
        <PartyList />
      </main>
    </div>
  )
}
