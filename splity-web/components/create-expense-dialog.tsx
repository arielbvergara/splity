"use client"

import type React from "react"

import { useState } from "react"
import { Plus, DollarSign, Users, Calendar } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Checkbox } from "@/components/ui/checkbox"
import type { Party, ExpenseCategory, ExpenseSplit, CreateExpenseInput } from "@/types"
import { expenseService } from "@/services/expense-service"
import { toast } from "@/hooks/use-toast"
import { formatCurrency } from "@/lib/utils"

interface CreateExpenseDialogProps {
  party: Party
  onExpenseCreated: () => void
}

export function CreateExpenseDialog({ party, onExpenseCreated }: CreateExpenseDialogProps) {
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  // Form state
  const [description, setDescription] = useState("")
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("USD")
  const [category, setCategory] = useState<ExpenseCategory>("other")
  const [paidBy, setPaidBy] = useState(party.members[0]?.userId || "")
  const [date, setDate] = useState(new Date().toISOString().split("T")[0])
  const [splitType, setSplitType] = useState<"equal" | "percentage" | "custom">("equal")

  // Split state
  const [selectedMembers, setSelectedMembers] = useState<Set<string>>(new Set(party.members.map((m) => m.userId)))
  const [customSplits, setCustomSplits] = useState<Record<string, number>>({})

  const handleMemberToggle = (userId: string) => {
    const newSelected = new Set(selectedMembers)
    if (newSelected.has(userId)) {
      newSelected.delete(userId)
    } else {
      newSelected.add(userId)
    }
    setSelectedMembers(newSelected)
  }

  const calculateSplits = (): ExpenseSplit[] => {
    const totalAmount = Number.parseFloat(amount) || 0
    const selectedMembersList = Array.from(selectedMembers)

    if (splitType === "equal") {
      const splitAmount = totalAmount / selectedMembersList.length
      return selectedMembersList.map((userId) => {
        const member = party.members.find((m) => m.userId === userId)
        return {
          userId,
          userName: member?.name || "",
          amount: splitAmount,
          paid: userId === paidBy,
        }
      })
    }

    if (splitType === "custom") {
      return selectedMembersList.map((userId) => {
        const member = party.members.find((m) => m.userId === userId)
        return {
          userId,
          userName: member?.name || "",
          amount: customSplits[userId] || 0,
          paid: userId === paidBy,
        }
      })
    }

    return []
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!description.trim() || !amount || selectedMembers.size === 0) {
      toast({
        title: "Missing information",
        description: "Please fill in all required fields",
        variant: "destructive",
      })
      return
    }

    const splits = calculateSplits()
    const totalSplit = splits.reduce((sum, split) => sum + split.amount, 0)
    const totalAmount = Number.parseFloat(amount)

    if (Math.abs(totalSplit - totalAmount) > 0.01) {
      toast({
        title: "Invalid split",
        description: "Split amounts must equal the total expense",
        variant: "destructive",
      })
      return
    }

    setLoading(true)
    try {
      const input: CreateExpenseInput = {
        partyId: party.id,
        description,
        amount: totalAmount,
        currency,
        category,
        paidBy,
        splitType,
        splits,
        date,
      }

      await expenseService.createExpense(input)
      toast({
        title: "Success",
        description: "Expense created successfully",
      })

      // Reset form
      setDescription("")
      setAmount("")
      setCategory("other")
      setDate(new Date().toISOString().split("T")[0])
      setSelectedMembers(new Set(party.members.map((m) => m.userId)))
      setCustomSplits({})

      setOpen(false)
      onExpenseCreated()
    } catch (error) {
      console.error("[v0] Failed to create expense:", error)
      toast({
        title: "Error",
        description: error instanceof Error ? error.message : "Failed to create expense",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const paidByMember = party.members.find((m) => m.userId === paidBy)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button className="gap-2">
          <Plus className="h-4 w-4" />
          Add Expense
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Add Expense</DialogTitle>
            <DialogDescription>Create a new expense and split it among members</DialogDescription>
          </DialogHeader>

          <div className="space-y-6 py-4">
            {/* Basic Info */}
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="description">Description</Label>
                <Input
                  id="description"
                  placeholder="Dinner at restaurant, Gas, Hotel..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="amount" className="flex items-center gap-2">
                  <DollarSign className="h-4 w-4" />
                  Amount
                </Label>
                <Input
                  id="amount"
                  type="number"
                  step="0.01"
                  placeholder="0.00"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="currency">Currency</Label>
                <Select value={currency} onValueChange={setCurrency}>
                  <SelectTrigger id="currency">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="USD">USD ($)</SelectItem>
                    <SelectItem value="EUR">EUR (€)</SelectItem>
                    <SelectItem value="GBP">GBP (£)</SelectItem>
                    <SelectItem value="JPY">JPY (¥)</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="category">Category</Label>
                <Select value={category} onValueChange={(v) => setCategory(v as ExpenseCategory)}>
                  <SelectTrigger id="category">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="food">Food & Dining</SelectItem>
                    <SelectItem value="transport">Transportation</SelectItem>
                    <SelectItem value="accommodation">Accommodation</SelectItem>
                    <SelectItem value="entertainment">Entertainment</SelectItem>
                    <SelectItem value="shopping">Shopping</SelectItem>
                    <SelectItem value="utilities">Utilities</SelectItem>
                    <SelectItem value="other">Other</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="date" className="flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  Date
                </Label>
                <Input id="date" type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
              </div>
            </div>

            {/* Paid By */}
            <div className="space-y-2">
              <Label htmlFor="paidBy">Paid By</Label>
              <Select value={paidBy} onValueChange={setPaidBy}>
                <SelectTrigger id="paidBy">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {party.members.map((member) => (
                    <SelectItem key={member.userId} value={member.userId}>
                      {member.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Split Configuration */}
            <div className="space-y-4">
              <Label className="flex items-center gap-2">
                <Users className="h-4 w-4" />
                Split Between
              </Label>

              <Tabs value={splitType} onValueChange={(v) => setSplitType(v as any)}>
                <TabsList className="grid w-full grid-cols-2">
                  <TabsTrigger value="equal">Equal Split</TabsTrigger>
                  <TabsTrigger value="custom">Custom Amounts</TabsTrigger>
                </TabsList>

                <TabsContent value="equal" className="space-y-3 mt-4">
                  {party.members.map((member) => (
                    <div key={member.userId} className="flex items-center justify-between rounded-lg border p-3">
                      <div className="flex items-center gap-3">
                        <Checkbox
                          checked={selectedMembers.has(member.userId)}
                          onCheckedChange={() => handleMemberToggle(member.userId)}
                        />
                        <span className="font-medium">{member.name}</span>
                      </div>
                      {selectedMembers.has(member.userId) && amount && (
                        <span className="text-sm text-muted-foreground">
                          {formatCurrency(Number.parseFloat(amount) / selectedMembers.size, currency)}
                        </span>
                      )}
                    </div>
                  ))}
                </TabsContent>

                <TabsContent value="custom" className="space-y-3 mt-4">
                  {party.members.map((member) => (
                    <div key={member.userId} className="flex items-center gap-3 rounded-lg border p-3">
                      <Checkbox
                        checked={selectedMembers.has(member.userId)}
                        onCheckedChange={() => handleMemberToggle(member.userId)}
                      />
                      <span className="flex-1 font-medium">{member.name}</span>
                      {selectedMembers.has(member.userId) && (
                        <Input
                          type="number"
                          step="0.01"
                          placeholder="0.00"
                          className="w-32"
                          value={customSplits[member.userId] || ""}
                          onChange={(e) =>
                            setCustomSplits({
                              ...customSplits,
                              [member.userId]: Number.parseFloat(e.target.value) || 0,
                            })
                          }
                        />
                      )}
                    </div>
                  ))}
                </TabsContent>
              </Tabs>
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? "Creating..." : "Create Expense"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
