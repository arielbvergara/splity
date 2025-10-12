"use client"

import { ArrowRight, CheckCircle2 } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import type { Party } from "@/types"
import { useAuth } from "@/contexts/auth-context"
import { settlementService } from "@/services/settlement-service"
import { formatCurrency, getInitials, getAvatarColor } from "@/lib/utils"

interface PartySettlementsProps {
  party: Party
}

export function PartySettlements({ party }: PartySettlementsProps) {
  const { user } = useAuth()
  const settlements = user
    ? settlementService.calculateSettlements(party, user.id)
    : { settlements: [], totalOwed: 0, totalOwing: 0 }

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">You're owed</p>
            <p className="mt-2 text-3xl font-bold text-success">{formatCurrency(settlements.totalOwed)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">You owe</p>
            <p className="mt-2 text-3xl font-bold text-destructive">{formatCurrency(settlements.totalOwing)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Settlements List */}
      <Card>
        <CardHeader>
          <CardTitle>Settlements</CardTitle>
        </CardHeader>
        <CardContent>
          {settlements.settlements.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border bg-muted/50 p-12 text-center">
              <CheckCircle2 className="mx-auto h-12 w-12 text-success" />
              <h3 className="mt-4 text-lg font-semibold text-foreground">All settled up!</h3>
              <p className="mt-2 text-sm text-muted-foreground">No outstanding balances in this party</p>
            </div>
          ) : (
            <div className="space-y-4">
              {settlements.settlements.map((settlement, index) => (
                <div key={index} className="flex items-center justify-between rounded-lg border border-border p-4">
                  <div className="flex items-center gap-4">
                    <Avatar className="h-10 w-10">
                      <AvatarFallback className={`${getAvatarColor(settlement.fromName)} text-white`}>
                        {getInitials(settlement.fromName)}
                      </AvatarFallback>
                    </Avatar>
                    <ArrowRight className="h-5 w-5 text-muted-foreground" />
                    <Avatar className="h-10 w-10">
                      <AvatarFallback className={`${getAvatarColor(settlement.toName)} text-white`}>
                        {getInitials(settlement.toName)}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="font-semibold text-foreground">
                        {settlement.fromName} pays {settlement.toName}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {formatCurrency(settlement.amount, settlement.currency)}
                      </p>
                    </div>
                  </div>
                  {user && settlement.from === user.id && <Button>Mark as Paid</Button>}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
