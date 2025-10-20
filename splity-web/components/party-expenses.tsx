"use client"

import { Receipt, Calendar, User, MoreVertical, Edit, Trash2 } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import type { Party } from "@/types"
import { formatCurrency, formatDate } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { ScanReceiptDialog } from "@/components/scan-receipt-dialog"
import { CreateExpenseDialog } from "@/components/create-expense-dialog"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"

interface PartyExpensesProps {
  party: Party
}

export function PartyExpenses({ party }: PartyExpensesProps) {
  const handleReceiptSaved = (receiptData: any) => {
    console.log("[v0] Receipt saved:", receiptData)
    // TODO: Create expense from receipt data
  }

  const handleExpenseCreated = () => {
    console.log("[v0] Expense created, refreshing party data")
    // TODO: Refresh party data
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Expenses ({party?.expenses?.length ?? 0})</CardTitle>
          <div className="flex gap-2">
            <ScanReceiptDialog partyId={party?.partyId} onReceiptSaved={handleReceiptSaved} />
            <CreateExpenseDialog party={party} onExpenseCreated={handleExpenseCreated} />
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {party?.expenses?.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-muted/50 p-12 text-center">
            <Receipt className="mx-auto h-12 w-12 text-muted-foreground" />
            <h3 className="mt-4 text-lg font-semibold text-foreground">No expenses yet</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Add your first expense or scan a receipt to get started
            </p>
          </div>
        ) : (
          <div className="space-y-3">
            {party.expenses?.map((expense) => (
              <div
                key={expense?.partyId}
                className="flex items-center justify-between rounded-lg border border-border p-4 transition-colors hover:bg-muted/50"
              >
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <h4 className="font-semibold text-foreground">{expense.description}</h4>
                    {expense.category && (
                      <Badge variant="secondary" className="capitalize">
                        {expense.category}
                      </Badge>
                    )}
                  </div>
                  <div className="mt-1 flex items-center gap-4 text-sm text-muted-foreground">
                    <div className="flex items-center gap-1">
                      <User className="h-3 w-3" />
                      Paid by {expense.paidByName}
                    </div>
                    <div className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      {formatDate(expense?.createdAt, "relative")}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="text-right">
                    <p className="text-lg font-bold text-foreground">
                      {formatCurrency(expense.amount, expense.currency)}
                    </p>
                    <p className="text-xs text-muted-foreground">Split {expense.splitType}</p>
                  </div>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon">
                        <MoreVertical className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem className="gap-2">
                        <Edit className="h-4 w-4" />
                        Edit
                      </DropdownMenuItem>
                      <DropdownMenuItem className="gap-2 text-destructive">
                        <Trash2 className="h-4 w-4" />
                        Delete
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
