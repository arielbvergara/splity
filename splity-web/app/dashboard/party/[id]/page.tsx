"use client"

import { use } from "react"
import { ArrowLeft, Users, Receipt, DollarSign, UserPlus } from "lucide-react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useParty } from "@/hooks/use-party"
import { Spinner } from "@/components/ui/spinner"
import { formatCurrency } from "@/lib/utils"
import { PartyMembers } from "@/components/party-members"
import { PartyExpenses } from "@/components/party-expenses"
import { PartySettlements } from "@/components/party-settlements"
import { PartySettings } from "@/components/party-settings"

interface PartyPageProps {
  params: Promise<{ id: string }>
}

export default function PartyPage({ params }: PartyPageProps) {
  const { id } = use(params)
  const { party, loading } = useParty(id)

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner className="h-8 w-8" />
      </div>
    )
  }

  if (!party) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-foreground">Party not found</h2>
          <p className="mt-2 text-muted-foreground">The party you're looking for doesn't exist.</p>
          <Button asChild className="mt-4">
            <Link href="/dashboard">Back to Dashboard</Link>
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-card">
        <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" asChild>
              <Link href="/dashboard">
                <ArrowLeft className="h-5 w-5" />
              </Link>
            </Button>
            <div className="flex-1">
              <h1 className="text-3xl font-bold text-foreground">{party.name}</h1>
              {party.description && <p className="mt-1 text-sm text-muted-foreground">{party.description}</p>}
            </div>
            <Button variant="outline" className="gap-2 bg-transparent">
              <UserPlus className="h-4 w-4" />
              Invite Members
            </Button>
          </div>

          {/* Quick Stats */}
          <div className="mt-6 grid gap-4 sm:grid-cols-3">
            <Card>
              <CardContent className="flex items-center gap-3 p-4">
                <div className="rounded-lg bg-primary/10 p-2">
                  <Users className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Members</p>
                  <p className="text-2xl font-bold text-foreground">{party?.members?.length}</p>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="flex items-center gap-3 p-4">
                <div className="rounded-lg bg-primary/10 p-2">
                  <Receipt className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Expenses</p>
                  <p className="text-2xl font-bold text-foreground">{party?.expenses?.length}</p>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="flex items-center gap-3 p-4">
                <div className="rounded-lg bg-primary/10 p-2">
                  <DollarSign className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Total</p>
                  <p className="text-2xl font-bold text-foreground">{formatCurrency(party?.totalExpenses || 0)}</p>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <Tabs defaultValue="expenses" className="space-y-6">
          <TabsList>
            <TabsTrigger value="expenses">Expenses</TabsTrigger>
            <TabsTrigger value="members">Members</TabsTrigger>
            <TabsTrigger value="settlements">Settlements</TabsTrigger>
            <TabsTrigger value="settings">Settings</TabsTrigger>
          </TabsList>

          <TabsContent value="expenses">
            <PartyExpenses party={party} />
          </TabsContent>

          <TabsContent value="members">
            <PartyMembers party={party} />
          </TabsContent>

          <TabsContent value="settlements">
            <PartySettlements party={party} />
          </TabsContent>

          <TabsContent value="settings">
            <PartySettings party={party} />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
