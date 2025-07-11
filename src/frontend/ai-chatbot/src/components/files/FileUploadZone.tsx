"use client"

import { useRef } from "react"
import { Upload, Paperclip, X } from "lucide-react"
import { Button } from "../ui/button"
import { Card } from "../ui/card"
import { FileAttachment } from "../../types/fileTypes"
import { FileAttachmentItem } from "./FileAttachmentItem"
import { cn } from "../../lib/utils"

interface FileUploadZoneProps {
  attachments: FileAttachment[]
  isDragActive: boolean
  onFileInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void
  onRemoveFile: (id: string) => void
  onClearAll: () => void
  dragHandlers: {
    onDragEnter: (e: React.DragEvent) => void
    onDragLeave: (e: React.DragEvent) => void
    onDragOver: (e: React.DragEvent) => void
    onDrop: (e: React.DragEvent) => void
  }
  canAddMoreFiles: boolean
  maxFiles: number
  className?: string
}

export function FileUploadZone({
  attachments,
  isDragActive,
  onFileInputChange,
  onRemoveFile,
  onClearAll,
  dragHandlers,
  canAddMoreFiles,
  maxFiles,
  className
}: FileUploadZoneProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleButtonClick = () => {
    if (canAddMoreFiles) {
      fileInputRef.current?.click()
    }
  }

  if (attachments.length === 0) {
    return (
      <Card
        className={cn(
          "relative border-2 border-dashed transition-all duration-200",
          isDragActive 
            ? "border-primary bg-primary/5 scale-[1.02]" 
            : "border-muted-foreground/25 hover:border-muted-foreground/50",
          className
        )}
        {...dragHandlers}
      >
        <div className="flex flex-col items-center justify-center p-6 text-center">
          <div className={cn(
            "rounded-full p-3 mb-3 transition-colors",
            isDragActive ? "bg-primary/10" : "bg-muted"
          )}>
            <Upload className={cn(
              "h-6 w-6 transition-colors",
              isDragActive ? "text-primary" : "text-muted-foreground"
            )} />
          </div>
          
          <div className="space-y-2">
            <p className="text-sm font-medium">
              {isDragActive ? "Drop files here" : "Drag files here or click to upload"}
            </p>
            <p className="text-xs text-muted-foreground">
              PDF, Word, Excel, CSV, Images up to 20MB each
            </p>
            <p className="text-xs text-muted-foreground">
              Maximum {maxFiles} files per message
            </p>
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={handleButtonClick}
            className="mt-4"
            disabled={!canAddMoreFiles}
          >
            <Paperclip className="h-4 w-4 mr-2" />
            Choose Files
          </Button>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          multiple
          className="hidden"
          onChange={onFileInputChange}
          accept=".pdf,.doc,.docx,.txt,.csv,.xls,.xlsx,.jpg,.jpeg,.png,.gif,.webp"
        />
      </Card>
    )
  }

  return (
    <Card className={cn("p-4", className)} {...dragHandlers}>
      {/* Header */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <Paperclip className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm font-medium">
            {attachments.length} file{attachments.length !== 1 ? 's' : ''} attached
          </span>
        </div>
        <div className="flex items-center gap-2">
          {canAddMoreFiles && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleButtonClick}
              className="h-7 px-2 text-xs"
            >
              Add More
            </Button>
          )}
          <Button
            variant="ghost"
            size="sm"
            onClick={onClearAll}
            className="h-7 px-2 text-xs text-muted-foreground hover:text-destructive"
          >
            <X className="h-3 w-3 mr-1" />
            Clear All
          </Button>
        </div>
      </div>

      {/* File List */}
      <div className="space-y-2">
        {attachments.map((attachment) => (
          <FileAttachmentItem
            key={attachment.id}
            attachment={attachment}
            onRemove={onRemoveFile}
          />
        ))}
      </div>

      {/* Drag Overlay */}
      {isDragActive && (
        <div className="absolute inset-0 bg-primary/5 border-2 border-primary border-dashed rounded-lg flex items-center justify-center">
          <div className="text-center">
            <Upload className="h-8 w-8 text-primary mx-auto mb-2" />
            <p className="text-sm font-medium text-primary">Drop files here</p>
          </div>
        </div>
      )}

      <input
        ref={fileInputRef}
        type="file"
        multiple
        className="hidden"
        onChange={onFileInputChange}
        accept=".pdf,.doc,.docx,.txt,.csv,.xls,.xlsx,.jpg,.jpeg,.png,.gif,.webp"
      />
    </Card>
  )
}
