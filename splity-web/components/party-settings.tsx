"use client"

import { Trash2, Edit, LinkIcon } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import type { Party } from "@/types"

interface PartySettingsProps {
  party: Party
}

export function PartySettings({ party }: PartySettingsProps) {
  return (
    <div className="space-y-6">
      {/* Party Details */}
      <Card>
        <CardHeader>
          <CardTitle>Party Details</CardTitle>
          <CardDescription>Update your party information</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="party-name">Party Name</Label>
            <Input id="party-name" defaultValue={party.name} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="party-description">Description</Label>
            <Input id="party-description" defaultValue={party.description} />
          </div>
          <Button className="gap-2">
            <Edit className="h-4 w-4" />
            Save Changes
          </Button>
        </CardContent>
      </Card>

      {/* Invite Link */}
      <Card>
        <CardHeader>
          <CardTitle>Invite Link</CardTitle>
          <CardDescription>Share this link to invite members</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex gap-2">
            <Input readOnly value={`https://splity.app/join/${party.inviteCode || "generating..."}`} />
            <Button variant="outline" className="gap-2 bg-transparent">
              <LinkIcon className="h-4 w-4" />
              Copy
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Danger Zone */}
      <Card className="border-destructive">
        <CardHeader>
          <CardTitle className="text-destructive">Danger Zone</CardTitle>
          <CardDescription>Irreversible actions</CardDescription>
        </CardHeader>
        <CardContent>
          <Button variant="destructive" className="gap-2">
            <Trash2 className="h-4 w-4" />
            Delete Party
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
