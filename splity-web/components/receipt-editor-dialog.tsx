"use client"

import { useState } from "react"
import { Calendar, DollarSign, Store, Edit } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import type { ReceiptData } from "@/types"
import { formatCurrency } from "@/lib/utils"

interface ReceiptEditorDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  receiptData: ReceiptData
  onSave: (data: ReceiptData) => void
}

export function ReceiptEditorDialog({ open, onOpenChange, receiptData, onSave }: ReceiptEditorDialogProps) {
  const [editedData, setEditedData] = useState<ReceiptData>(receiptData)

  const handleSave = () => {
    onSave(editedData)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Review Receipt Data</DialogTitle>
          <DialogDescription>
            AI has extracted the following information. Review and edit if needed.
            {receiptData.confidence && (
              <Badge variant="secondary" className="ml-2">
                {Math.round(receiptData.confidence * 100)}% confidence
              </Badge>
            )}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Merchant Name */}
          <div className="space-y-2">
            <Label htmlFor="merchant" className="flex items-center gap-2">
              <Store className="h-4 w-4" />
              Merchant Name
            </Label>
            <Input
              id="merchant"
              value={editedData.merchantName || ""}
              onChange={(e) => setEditedData({ ...editedData, merchantName: e.target.value })}
              placeholder="Enter merchant name"
            />
          </div>

          {/* Date */}
          <div className="space-y-2">
            <Label htmlFor="date" className="flex items-center gap-2">
              <Calendar className="h-4 w-4" />
              Date
            </Label>
            <Input
              id="date"
              type="date"
              value={editedData.date || ""}
              onChange={(e) => setEditedData({ ...editedData, date: e.target.value })}
            />
          </div>

          {/* Total Amount */}
          <div className="space-y-2">
            <Label htmlFor="total" className="flex items-center gap-2">
              <DollarSign className="h-4 w-4" />
              Total Amount
            </Label>
            <Input
              id="total"
              type="number"
              step="0.01"
              value={editedData.total || ""}
              onChange={(e) => setEditedData({ ...editedData, total: Number.parseFloat(e.target.value) })}
              placeholder="0.00"
            />
          </div>

          {/* Tax */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="tax">Tax</Label>
              <Input
                id="tax"
                type="number"
                step="0.01"
                value={editedData.tax || ""}
                onChange={(e) => setEditedData({ ...editedData, tax: Number.parseFloat(e.target.value) })}
                placeholder="0.00"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="tip">Tip</Label>
              <Input
                id="tip"
                type="number"
                step="0.01"
                value={editedData.tip || ""}
                onChange={(e) => setEditedData({ ...editedData, tip: Number.parseFloat(e.target.value) })}
                placeholder="0.00"
              />
            </div>
          </div>

          {/* Items */}
          {editedData.items && editedData.items.length > 0 && (
            <div className="space-y-2">
              <Label className="flex items-center gap-2">
                <Edit className="h-4 w-4" />
                Items ({editedData.items.length})
              </Label>
              <div className="rounded-lg border border-border">
                <div className="max-h-48 overflow-y-auto">
                  {editedData.items.map((item, index) => (
                    <div
                      key={index}
                      className="flex items-center justify-between border-b border-border p-3 last:border-0"
                    >
                      <div className="flex-1">
                        <p className="text-sm font-medium text-foreground">{item.description}</p>
                        <p className="text-xs text-muted-foreground">
                          Qty: {item.quantity} × {formatCurrency(item.price)}
                        </p>
                      </div>
                      <p className="text-sm font-semibold text-foreground">{formatCurrency(item.total)}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave}>Save & Continue</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
