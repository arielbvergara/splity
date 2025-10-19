"use client"

import { Search } from "lucide-react"
import { useState } from "react"
import { Input } from "@/components/ui/input"
import { PartyCard } from "@/components/party-card"
import { CreatePartyDialog } from "@/components/create-party-dialog"
import { useParties } from "@/hooks/use-parties"
import { Spinner } from "@/components/ui/spinner"

export function PartyList() {
  const { parties, loading } = useParties()
  const [searchQuery, setSearchQuery] = useState("")

  const filteredParties = parties.filter((party) => party.name.toLowerCase().includes(searchQuery.toLowerCase()))

  return (
    <div className="space-y-6">
      {/* Search and Create */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative flex-1 sm:max-w-md">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search parties..."
            className="pl-9"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
        <CreatePartyDialog />
      </div>

      {/* Loading State */}
      {loading && (
        <div className="flex items-center justify-center py-12">
          <Spinner className="h-8 w-8" />
        </div>
      )}

      {/* Empty State */}
      {!loading && filteredParties.length === 0 && (
        <div className="rounded-lg border border-dashed border-border bg-muted/50 p-12 text-center">
          <h3 className="text-lg font-semibold text-foreground">No parties found</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            {searchQuery ? "Try adjusting your search query" : "Create your first party to start splitting expenses"}
          </p>
        </div>
      )}

      {/* Party Cards */}
      {!loading && filteredParties.length > 0 && (
        <div className="grid gap-6 md:grid-cols-2">
          {filteredParties.map((party) => (
            <PartyCard key={party.partyId} party={party} />
          ))}
        </div>
      )}
    </div>
  )
}
