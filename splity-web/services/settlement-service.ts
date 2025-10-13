// Settlement calculation service

import type { Party, Settlement, PartySettlements } from "@/types"

export const settlementService = {
  calculateSettlements(party: Party, currentUserId: string): PartySettlements {
    // Calculate who owes whom
    const balances = new Map<string, number>()

    // Initialize balances for all members
    party.members?.forEach((member) => {
      balances.set(member.userId, 0)
    })

    // Calculate net balance for each member
    party.expenses?.forEach((expense) => {
      // Person who paid gets positive balance
      const currentBalance = balances.get(expense.payerId) || 0
      balances.set(expense.payerId, currentBalance + expense.amount)

      // People who owe get negative balance
      expense.splits?.forEach((split) => {
        if (!split.paid) {
          const splitBalance = balances.get(split.userId) || 0
          balances.set(split.userId, splitBalance - split.amount)
        }
      })
    })

    // Simplify settlements using greedy algorithm
    const settlements: Settlement[] = []
    const debtors: Array<{ userId: string; amount: number }> = []
    const creditors: Array<{ userId: string; amount: number }> = []

    balances.forEach((balance, userId) => {
      const member = party.members?.find((m) => m.userId === userId)
      if (!member) return

      if (balance < -0.01) {
        debtors.push({ userId, amount: Math.abs(balance) })
      } else if (balance > 0.01) {
        creditors.push({ userId, amount: balance })
      }
    })

    // Sort by amount (largest first)
    debtors.sort((a, b) => b.amount - a.amount)
    creditors.sort((a, b) => b.amount - a.amount)

    // Match debtors with creditors
    let i = 0
    let j = 0
    while (i < debtors.length && j < creditors.length) {
      const debtor = debtors[i]
      const creditor = creditors[j]
      const amount = Math.min(debtor.amount, creditor.amount)

      const debtorMember = party.members?.find((m) => m.userId === debtor.userId)
      const creditorMember = party.members?.find((m) => m.userId === creditor.userId)

      if (debtorMember && creditorMember) {
        settlements.push({
          from: debtor.userId,
          fromName: debtorMember.name,
          to: creditor.userId,
          toName: creditorMember.name,
          amount,
          currency: "EUR"
        })
      }

      debtor.amount -= amount
      creditor.amount -= amount

      if (debtor.amount < 0.01) i++
      if (creditor.amount < 0.01) j++
    }

    // Calculate totals for current user
    let totalOwed = 0
    let totalOwing = 0

    settlements.forEach((settlement) => {
      if (settlement.to === currentUserId) {
        totalOwed += settlement.amount
      }
      if (settlement.from === currentUserId) {
        totalOwing += settlement.amount
      }
    })

    return {
      partyId: party.partyId,
      settlements,
      totalOwed,
      totalOwing,
    }
  },

  getSettlementsForUser(settlements: Settlement[], userId: string) {
    return settlements.filter((s) => s.from === userId || s.to === userId)
  },
}
