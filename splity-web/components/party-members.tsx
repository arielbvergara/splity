"use client"

import { UserPlus, Mail, Crown, MoreVertical } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import type { Party } from "@/types"
import { getInitials, getAvatarColor, formatDate } from "@/lib/utils"

interface PartyMembersProps {
  party: Party
}

export function PartyMembers({ party }: PartyMembersProps) {
  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Members ({party.members.length})</CardTitle>
          <Button className="gap-2">
            <UserPlus className="h-4 w-4" />
            Add Member
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {party.members.map((member) => (
            <div key={member.userId} className="flex items-center justify-between rounded-lg border border-border p-4">
              <div className="flex items-center gap-3">
                <Avatar className="h-12 w-12">
                  <AvatarFallback className={`${getAvatarColor(member.name)} text-white`}>
                    {getInitials(member.name)}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <div className="flex items-center gap-2">
                    <p className="font-semibold text-foreground">{member.name}</p>
                    {member.role === "owner" && <Crown className="h-4 w-4 text-amber-500" />}
                  </div>
                  <div className="flex items-center gap-1 text-sm text-muted-foreground">
                    <Mail className="h-3 w-3" />
                    {member.email}
                  </div>
                  <p className="text-xs text-muted-foreground">Joined {formatDate(member.joinedAt, "relative")}</p>
                </div>
              </div>

              {member.role !== "owner" && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem className="text-destructive">Remove from party</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}
