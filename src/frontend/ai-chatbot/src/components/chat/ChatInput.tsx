"use client"

import { useState, useRef, useEffect, FormEvent, KeyboardEvent } from "react"
import { Send, Mic, Paperclip, ArrowUp } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Container } from "@/components/layout/Container"
import { QUICK_PROMPTS } from "@/lib/constants"
import { cn } from "@/lib/utils"
import { useIsMobile } from "@/hooks/useBreakpoint"
import { FileUploadZone } from "../files/FileUploadZone"
import { useFileUpload } from "../../hooks/useFileUpload"
import { FileAttachment } from "../../types/fileTypes"

interface ChatInputProps {
  input: string
  isLoading: boolean
  onInputChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void
  onSend: (e: FormEvent<HTMLFormElement>, attachments?: FileAttachment[]) => void
  className?: string
}

export function ChatInput({ 
  input, 
  isLoading, 
  onInputChange, 
  onSend,
  className 
}: ChatInputProps) {
  const isMobile = useIsMobile()
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const [rows, setRows] = useState(1)
  const [showFileUpload, setShowFileUpload] = useState(false)
  
  // File upload hook
  const fileUploadHook = useFileUpload({
    maxFiles: 2 // Updated to match backend limit
  })

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      const textarea = textareaRef.current
      textarea.style.height = 'auto'
      const newHeight = Math.min(textarea.scrollHeight, 120) // Max 120px height
      textarea.style.height = `${newHeight}px`
      
      // Calculate rows based on height
      const lineHeight = 24 // Approximate line height
      const newRows = Math.min(Math.max(1, Math.floor(newHeight / lineHeight)), 5)
      setRows(newRows)
    }
  }, [input])

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      if ((input.trim() || fileUploadHook.attachments.length > 0) && !isLoading) {
        const formEvent = new Event('submit', { bubbles: true, cancelable: true }) as unknown as FormEvent<HTMLFormElement>
        handleSubmit(formEvent)
      }
    }
  }

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    if ((input.trim() || fileUploadHook.attachments.length > 0) && !isLoading) {
      onSend(e, fileUploadHook.attachments)
      // Clear attachments after sending
      fileUploadHook.clearFiles()
    }
  }

  const handleQuickPrompt = (prompt: string) => {
    onInputChange({ target: { value: prompt } } as React.ChangeEvent<HTMLTextAreaElement>)
    // Focus the textarea after setting the prompt
    setTimeout(() => {
      textareaRef.current?.focus()
    }, 0)
  }

  const canSend = (input.trim() || fileUploadHook.attachments.length > 0) && !isLoading

  return (
    <div className={cn(
      "border-t bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/80",
      className
    )}>
      <Container size="lg">
        <div className="py-3 md:py-4 space-y-3 md:space-y-4 pb-safe">
          {/* Quick Action Prompts */}
          {!input && (
            <div className="flex flex-wrap gap-2">
              {QUICK_PROMPTS.slice(0, isMobile ? 2 : 4).map((prompt, index) => (
                <Button
                  key={index}
                  variant="outline"
                  size="sm"
                  onClick={() => handleQuickPrompt(prompt.prompt)}
                  disabled={isLoading}
                  className="text-xs md:text-sm h-8 md:h-9 px-3 text-muted-foreground hover:text-foreground transition-colors"
                >
                  {prompt.title}
                </Button>
              ))}
              {QUICK_PROMPTS.length > (isMobile ? 2 : 4) && (
                <Badge variant="secondary" className="text-xs">
                  +{QUICK_PROMPTS.length - (isMobile ? 2 : 4)} more
                </Badge>
              )}
            </div>
          )}

          {/* Input Form */}
          <form onSubmit={handleSubmit} className="relative">
            {/* File Upload Zone */}
            {showFileUpload && (
              <div className="mb-3 p-3 border rounded-lg bg-background/50">
                <FileUploadZone 
                  attachments={fileUploadHook.attachments}
                  isDragActive={fileUploadHook.isDragActive}
                  onFileInputChange={(e) => {
                    if (e.target.files) {
                      fileUploadHook.addFiles(Array.from(e.target.files))
                    }
                  }}
                  onRemoveFile={fileUploadHook.removeFile}
                  onClearAll={fileUploadHook.clearFiles}
                  dragHandlers={fileUploadHook.dragHandlers}
                  canAddMoreFiles={fileUploadHook.attachments.length < 5}
                  maxFiles={5}
                />
              </div>
            )}

            <div className="relative flex items-end space-x-2 md:space-x-3">
              {/* Main Input Area */}
              <div className="relative flex-1">
                <Textarea
                  ref={textareaRef}
                  value={input}
                  onChange={onInputChange}
                  onKeyDown={handleKeyDown}
                  placeholder={isMobile 
                    ? "Ask about your fleet..." 
                    : "Type your message... (Press Enter to send, Shift+Enter for new line)"
                  }
                  disabled={isLoading}
                  rows={rows}
                  className={cn(
                    "min-h-[44px] max-h-[120px] resize-none pr-12 md:pr-16",
                    "text-sm md:text-base",
                    "placeholder:text-muted-foreground/70"
                  )}
                  style={{ 
                    height: 'auto'
                  }}
                />

                {/* Input Actions */}
                <div className="absolute right-2 bottom-2 flex items-center space-x-1">
                  {/* Attachment Button */}
                  <div className="relative">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => setShowFileUpload(!showFileUpload)}
                      className={cn(
                        "h-8 w-8 text-muted-foreground hover:text-foreground transition-colors",
                        showFileUpload && "text-foreground",
                        fileUploadHook.attachments.length > 0 && "text-primary"
                      )}
                    >
                      <Paperclip className="h-4 w-4" />
                      <span className="sr-only">
                        {showFileUpload ? "Hide file upload" : "Attach files"}
                      </span>
                    </Button>
                    {fileUploadHook.attachments.length > 0 && (
                      <Badge 
                        variant="secondary" 
                        className="absolute -top-1 -right-1 h-4 w-4 p-0 text-xs flex items-center justify-center"
                      >
                        {fileUploadHook.attachments.length}
                      </Badge>
                    )}
                  </div>

                  {/* Voice Input Button (Future) */}
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground hover:text-foreground opacity-50 cursor-not-allowed"
                    disabled
                  >
                    <Mic className="h-4 w-4" />
                    <span className="sr-only">Voice input (Coming soon)</span>
                  </Button>
                </div>

                {/* Character count / hint */}
                {input.length > 0 && (
                  <div className="absolute -bottom-6 left-0 text-xs text-muted-foreground">
                    {input.length} characters
                    {!isMobile && " • Press Enter to send"}
                  </div>
                )}
              </div>

              {/* Send Button */}
              <Button
                type="submit"
                disabled={!canSend}
                size={isMobile ? "icon" : "default"}
                className={cn(
                  "transition-all duration-200",
                  canSend ? "scale-100" : "scale-95 opacity-50",
                  isMobile ? "h-11 w-11" : "h-11 px-6"
                )}
              >
                {isLoading ? (
                  <div className="flex items-center space-x-2">
                    <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
                    {!isMobile && <span className="text-sm">Sending</span>}
                  </div>
                ) : (
                  <div className="flex items-center space-x-2">
                    {isMobile ? (
                      <ArrowUp className="h-5 w-5" />
                    ) : (
                      <>
                        <Send className="h-4 w-4" />
                        <span className="text-sm font-medium">Send</span>
                      </>
                    )}
                  </div>
                )}
              </Button>
            </div>
          </form>

          {/* Mobile hint */}
          {isMobile && input.length > 0 && (
            <div className="text-xs text-muted-foreground text-center">
              Tap send or press Enter to submit
            </div>
          )}
        </div>
      </Container>
    </div>
  )
}
