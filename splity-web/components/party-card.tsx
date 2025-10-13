"use client"

import { Users } from "lucide-react"
import Link from "next/link"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import type { Party } from "@/types"
import { formatCurrency, getInitials, getAvatarColor } from "@/lib/utils"
import { useCognitoAuth } from "@/contexts/cognito-auth-context"
import { settlementService } from "@/services/settlement-service"

interface PartyCardProps {
  party: Party
}

export function PartyCard({ party }: PartyCardProps) {
  const { user } = useCognitoAuth()

  // Calculate user's balance
  const settlements = user ? settlementService.calculateSettlements(party, user.userId) : { totalOwed: 0, totalOwing: 0 }

  const userBalance = settlements.totalOwed - settlements.totalOwing
  const isOwed = userBalance > 0

  return (
    <Link href={`/dashboard/party/${party.partyId}`}>
      <Card className="border-border bg-card transition-shadow hover:shadow-md cursor-pointer">
        <CardContent className="p-6">
          {/* Header */}
          <div className="mb-4 flex items-start justify-between">
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-card-foreground">{party.name}</h3>
              <div className="mt-1 flex items-center gap-1.5 text-sm text-muted-foreground">
                <Users className="h-4 w-4" />
                <span>{party.members.length} members</span>
              </div>
            </div>

            {/* Member Avatars */}
            <div className="flex -space-x-2">
              {party.members.slice(0, 3).map((member, index) => (
                <Avatar key={index} className="h-9 w-9 border-2 border-card">
                  <AvatarFallback className={`${getAvatarColor(member.name)} text-xs font-medium text-white`}>
                    {getInitials(member.name)}
                  </AvatarFallback>
                </Avatar>
              ))}
              {party.members.length > 3 && (
                <Avatar className="h-9 w-9 border-2 border-card">
                  <AvatarFallback className="bg-muted text-xs font-medium text-muted-foreground">
                    +{party.members.length - 3}
                  </AvatarFallback>
                </Avatar>
              )}
            </div>
          </div>

          {/* Expenses and Balance */}
          <div className="flex items-end justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Total expenses</p>
              <p className="mt-1 text-2xl font-bold text-card-foreground">{formatCurrency(party.totalExpenses)}</p>
            </div>

            {Math.abs(userBalance) > 0.01 && (
              <Button
                className={
                  isOwed
                    ? "bg-success text-success-foreground hover:bg-success/90"
                    : "bg-destructive text-destructive-foreground hover:bg-destructive/90"
                }
                onClick={(e) => {
                  e.preventDefault()
                  // Navigate to settlements
                }}
              >
                {isOwed ? "You're owed" : "You owe"} {formatCurrency(Math.abs(userBalance))}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    </Link>
  )
}
