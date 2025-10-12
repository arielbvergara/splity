// Core entity types for Splity application
// Types match backend C# models

export interface User {
  userId: string
  name: string
  email: string
  avatar?: string
  cognitoUserId?: string
  createdAt: string
}

// Backend response wrapper for User endpoints
export interface UserWithDetails {
  user: {
    userId: string
    name: string
    email: string
    createdAt: string
    ownedParties: Party[]
    paidExpenses: Expense[]
    partyContributions: PartyContributor[]
    expenseParticipators: ExpenseParticipant[]
  }
}

export interface Party {
  partyId: string
  ownerId: string
  name: string
  description?: string
  createdAt: string
  owner?: User
  expenses?: Expense[]
  contributors?: PartyContributor[]
  billImages?: PartyBillsImage[]
}

// Additional backend models
export interface PartyContributor {
  contributorId: string
  partyId: string
  userId: string
  contributedAmount: number
  createdAt: string
}

export interface PartyBillsImage {
  billImageId: string
  partyId: string
  imageUrl: string
  createdAt: string
}

export interface ExpenseParticipant {
  participantId: string
  expenseId: string
  userId: string
  owedAmount: number
  createdAt: string
}

export interface PartyMember {
  userId: string
  name: string
  email: string
  avatar?: string
  role: "owner" | "member"
  joinedAt: string
}

export interface Expense {
  expenseId: string
  partyId: string
  payerId: string
  description: string
  amount: number
  createdAt: string
  // Extended fields for frontend
  currency?: string
  category?: ExpenseCategory
  paidByName?: string
  splitType?: "equal" | "percentage" | "amount" | "custom"
  splits?: ExpenseSplit[]
  receiptUrl?: string
  receiptData?: ReceiptData
}

export interface ExpenseSplit {
  userId: string
  userName: string
  amount: number
  percentage?: number
  paid: boolean
}

export interface ReceiptData {
  merchantName?: string
  date?: string
  total?: number
  items?: ReceiptItem[]
  tax?: number
  tip?: number
  confidence?: number
}

export interface ReceiptItem {
  description: string
  quantity: number
  price: number
  total: number
}

export type ExpenseCategory =
  | "food"
  | "transport"
  | "accommodation"
  | "entertainment"
  | "shopping"
  | "utilities"
  | "other"

export interface Settlement {
  from: string
  fromName: string
  to: string
  toName: string
  amount: number
  currency: string
}

export interface PartySettlements {
  partyId: string
  settlements: Settlement[]
  totalOwed: number
  totalOwing: number
}

// API Response types - Backend format
export interface BackendApiResponse<T> {
  success: boolean
  data: T | null
  errorMessage: string | null
}

export interface ApiError {
  errorMessage: string
  success: false
  data: null
}

// Form types - matching backend expectations
export interface CreatePartyInput {
  ownerId: string
  name: string
  description?: string
}

export interface CreateUserInput {
  name: string
  email: string
  cognitoUserId?: string
}

export interface CreateExpenseInput {
  partyId: string
  payerId: string
  description: string
  amount: number
  // Extended fields for frontend
  currency?: string
  category?: ExpenseCategory
  splitType?: "equal" | "percentage" | "amount" | "custom"
  splits?: ExpenseSplit[]
}

export interface UpdateUserInput {
  name?: string
  email?: string
  avatar?: string
}

// Stats types
export interface DashboardStats {
  totalExpenses: number
  activeParties: number
  youreOwed: number
  totalReceipts: number
  monthlyChange: {
    expenses: number
    owed: number
  }
}
