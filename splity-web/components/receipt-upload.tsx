"use client"

import type React from "react"

import { useState, useCallback } from "react"
import { Upload, FileImage, X, Loader2 } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { expenseService } from "@/services/expense-service"
import { toast } from "@/hooks/use-toast"
import type { ReceiptData } from "@/types"

interface ReceiptUploadProps {
  partyId: string
  onReceiptProcessed: (data: ReceiptData) => void
}

export function ReceiptUpload({ partyId, onReceiptProcessed }: ReceiptUploadProps) {
  const [isDragging, setIsDragging] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [processing, setProcessing] = useState(false)

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)

    const droppedFile = e.dataTransfer.files[0]
    if (droppedFile && droppedFile.type.startsWith("image/")) {
      handleFileSelect(droppedFile)
    } else {
      toast({
        title: "Invalid file",
        description: "Please upload an image file",
        variant: "destructive",
      })
    }
  }, [])

  const handleFileSelect = (selectedFile: File) => {
    setFile(selectedFile)

    // Create preview
    const reader = new FileReader()
    reader.onloadend = () => {
      setPreview(reader.result as string)
    }
    reader.readAsDataURL(selectedFile)
  }

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      handleFileSelect(selectedFile)
    }
  }

  const handleUpload = async () => {
    if (!file) return

    setProcessing(true)
    try {
      const receiptData = await expenseService.uploadReceipt(partyId, file)
      toast({
        title: "Receipt processed",
        description: "AI has extracted the receipt data",
      })
      onReceiptProcessed(receiptData)
    } catch (error) {
      console.error("[v0] Receipt upload failed:", error)
      toast({
        title: "Upload failed",
        description: error instanceof Error ? error.message : "Failed to process receipt",
        variant: "destructive",
      })
    } finally {
      setProcessing(false)
    }
  }

  const handleClear = () => {
    setFile(null)
    setPreview(null)
  }

  return (
    <Card>
      <CardContent className="p-6">
        {!file ? (
          <div
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={`relative flex min-h-[300px] cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed transition-colors ${
              isDragging
                ? "border-primary bg-primary/5"
                : "border-border bg-muted/50 hover:border-primary/50 hover:bg-muted"
            }`}
          >
            <input
              type="file"
              accept="image/*"
              onChange={handleFileInput}
              className="absolute inset-0 cursor-pointer opacity-0"
            />
            <Upload className="h-12 w-12 text-muted-foreground" />
            <h3 className="mt-4 text-lg font-semibold text-foreground">Upload Receipt</h3>
            <p className="mt-2 text-sm text-muted-foreground">Drag and drop or click to browse</p>
            <p className="mt-1 text-xs text-muted-foreground">Supports JPG, PNG, HEIC</p>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="relative">
              {preview && (
                <img
                  src={preview || "/placeholder.svg"}
                  alt="Receipt preview"
                  className="h-64 w-full rounded-lg object-contain bg-muted"
                />
              )}
              <Button
                variant="destructive"
                size="icon"
                className="absolute right-2 top-2"
                onClick={handleClear}
                disabled={processing}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FileImage className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium text-foreground">{file.name}</p>
                  <p className="text-xs text-muted-foreground">{(file.size / 1024).toFixed(1)} KB</p>
                </div>
              </div>

              <Button onClick={handleUpload} disabled={processing} className="gap-2">
                {processing ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Processing...
                  </>
                ) : (
                  "Process Receipt"
                )}
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
