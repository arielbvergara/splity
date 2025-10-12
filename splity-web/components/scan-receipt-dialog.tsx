"use client"

import { useState } from "react"
import { Receipt } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { ReceiptUpload } from "@/components/receipt-upload"
import { ReceiptEditorDialog } from "@/components/receipt-editor-dialog"
import type { ReceiptData } from "@/types"

interface ScanReceiptDialogProps {
  partyId: string
  onReceiptSaved: (data: ReceiptData) => void
}

export function ScanReceiptDialog({ partyId, onReceiptSaved }: ScanReceiptDialogProps) {
  const [uploadOpen, setUploadOpen] = useState(false)
  const [editorOpen, setEditorOpen] = useState(false)
  const [receiptData, setReceiptData] = useState<ReceiptData | null>(null)

  const handleReceiptProcessed = (data: ReceiptData) => {
    setReceiptData(data)
    setUploadOpen(false)
    setEditorOpen(true)
  }

  const handleReceiptSaved = (data: ReceiptData) => {
    onReceiptSaved(data)
    setEditorOpen(false)
    setReceiptData(null)
  }

  return (
    <>
      <Dialog open={uploadOpen} onOpenChange={setUploadOpen}>
        <DialogTrigger asChild>
          <Button variant="outline" className="gap-2 bg-transparent">
            <Receipt className="h-4 w-4" />
            Scan Receipt
          </Button>
        </DialogTrigger>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Scan Receipt</DialogTitle>
            <DialogDescription>Upload a receipt image and let AI extract the expense details</DialogDescription>
          </DialogHeader>
          <ReceiptUpload partyId={partyId} onReceiptProcessed={handleReceiptProcessed} />
        </DialogContent>
      </Dialog>

      {receiptData && (
        <ReceiptEditorDialog
          open={editorOpen}
          onOpenChange={setEditorOpen}
          receiptData={receiptData}
          onSave={handleReceiptSaved}
        />
      )}
    </>
  )
}
