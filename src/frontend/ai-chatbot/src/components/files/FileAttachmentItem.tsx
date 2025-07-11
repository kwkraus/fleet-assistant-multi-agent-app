"use client"

import { X, FileText, Image as ImageIcon } from "lucide-react"
import { FileAttachment, getFileIcon, formatFileSize } from "../../types/fileTypes"
import { Button } from "../ui/button"
import { Badge } from "../ui/badge"
import { cn } from "../../lib/utils"

interface FileAttachmentItemProps {
  attachment: FileAttachment
  onRemove: (id: string) => void
  className?: string
}

export function FileAttachmentItem({ attachment, onRemove, className }: FileAttachmentItemProps) {
  const isImage = attachment.type.startsWith('image/')
  
  return (
    <div className={cn(
      "flex items-center gap-3 p-3 bg-muted/50 rounded-lg border group hover:bg-muted/70 transition-colors",
      className
    )}>
      {/* File Icon/Preview */}
      <div className="flex-shrink-0">
        {isImage && attachment.preview ? (
          <div className="relative">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img 
              src={attachment.preview} 
              alt={attachment.name}
              className="w-10 h-10 object-cover rounded border"
            />
            <div className="absolute inset-0 bg-black/0 group-hover:bg-black/10 rounded transition-colors" />
          </div>
        ) : (
          <div className="w-10 h-10 bg-background rounded border flex items-center justify-center">
            {isImage ? (
              <ImageIcon className="h-5 w-5 text-muted-foreground" />
            ) : (
              <FileText className="h-5 w-5 text-muted-foreground" />
            )}
          </div>
        )}
      </div>

      {/* File Info */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-sm font-medium truncate" title={attachment.name}>
            {attachment.name}
          </span>
          <span className="text-xs">{getFileIcon(attachment.type)}</span>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="text-xs px-1.5 py-0.5">
            {formatFileSize(attachment.size)}
          </Badge>
          {attachment.type && (
            <span className="text-xs text-muted-foreground">
              {attachment.type.split('/')[1]?.toUpperCase()}
            </span>
          )}
        </div>
        
        {/* Upload Progress (if applicable) */}
        {attachment.uploadProgress !== undefined && attachment.uploadProgress < 100 && (
          <div className="mt-2">
            <div className="flex items-center gap-2">
              <div className="flex-1 bg-muted rounded-full h-1">
                <div 
                  className="bg-primary h-1 rounded-full transition-all duration-300"
                  style={{ width: `${attachment.uploadProgress}%` }}
                />
              </div>
              <span className="text-xs text-muted-foreground">
                {attachment.uploadProgress}%
              </span>
            </div>
          </div>
        )}

        {/* Error State */}
        {attachment.error && (
          <div className="mt-1">
            <span className="text-xs text-destructive">{attachment.error}</span>
          </div>
        )}
      </div>

      {/* Remove Button */}
      <Button
        variant="ghost"
        size="sm"
        onClick={() => onRemove(attachment.id)}
        className="h-8 w-8 p-0 opacity-0 group-hover:opacity-100 transition-opacity"
      >
        <X className="h-4 w-4" />
      </Button>
    </div>
  )
}
