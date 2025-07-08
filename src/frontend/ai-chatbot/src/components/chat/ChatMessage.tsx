"use client"

import { useState } from "react"
import { Copy, CheckCheck, User, Bot } from "lucide-react"
import ReactMarkdown from "react-markdown"
import remarkGfm from 'remark-gfm'
import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import { formatTime } from "@/lib/utils"

interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  createdAt?: Date
}

interface ChatMessageProps {
  message: ChatMessage
  isLastMessage?: boolean
  className?: string
}

export function ChatMessage({ message, isLastMessage, className }: ChatMessageProps) {
  const [copied, setCopied] = useState(false)
  const isUser = message.role === 'user'
  const isAssistant = message.role === 'assistant'

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(message.content)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      console.error('Failed to copy text:', err)
    }
  }

  return (
    <div className={cn(
      "group flex w-full gap-3 px-3 py-4 md:px-4",
      isUser ? "justify-end" : "justify-start",
      className
    )}>
      {/* Assistant Avatar (Left side) */}
      {isAssistant && (
        <div className="flex-shrink-0">
          <Avatar className="h-8 w-8 border">
            <AvatarFallback className="bg-primary text-primary-foreground text-xs font-semibold">
              <Bot className="h-4 w-4" />
            </AvatarFallback>
          </Avatar>
        </div>
      )}

      {/* Message Content */}
      <div className={cn(
        "flex max-w-[85%] md:max-w-[75%] flex-col",
        isUser ? "items-end" : "items-start"
      )}>
        {/* Message Bubble */}
        <Card className={cn(
          "relative w-full shadow-sm transition-all duration-200",
          isUser 
            ? "bg-primary text-primary-foreground border-primary" 
            : "bg-card border-border hover:shadow-md",
          isLastMessage && "animate-in slide-in-from-bottom-2 duration-300"
        )}>
          <CardContent className="p-3 md:p-4">
            {isAssistant ? (
              <div className="prose prose-sm dark:prose-invert max-w-none">
                <ReactMarkdown
                  remarkPlugins={[remarkGfm]}
                  components={{
                    p: ({ children }) => (
                      <p className="mb-2 last:mb-0 leading-relaxed text-sm md:text-base">
                        {children}
                      </p>
                    ),
                    ul: ({ children }) => (
                      <ul className="list-disc list-inside mb-2 space-y-1 text-sm md:text-base">
                        {children}
                      </ul>
                    ),
                    ol: ({ children }) => (
                      <ol className="list-decimal list-inside mb-2 space-y-1 text-sm md:text-base">
                        {children}
                      </ol>
                    ),
                    li: ({ children }) => (
                      <li className="text-sm md:text-base">{children}</li>
                    ),
                    code: ({ children }) => (
                      <code className="bg-muted px-1.5 py-0.5 rounded text-xs md:text-sm font-mono border">
                        {children}
                      </code>
                    ),
                    pre: ({ children }) => (
                      <pre className="bg-muted p-3 rounded-lg text-xs md:text-sm overflow-x-auto mb-2 border">
                        {children}
                      </pre>
                    ),
                    strong: ({ children }) => (
                      <strong className="font-semibold">{children}</strong>
                    ),
                    em: ({ children }) => (
                      <em className="italic">{children}</em>
                    ),
                    blockquote: ({ children }) => (
                      <blockquote className="border-l-4 border-border pl-4 italic my-2">
                        {children}
                      </blockquote>
                    ),
                    h1: ({ children }) => (
                      <h1 className="text-lg md:text-xl font-bold mb-3 mt-4 first:mt-0">
                        {children}
                      </h1>
                    ),
                    h2: ({ children }) => (
                      <h2 className="text-base md:text-lg font-bold mb-2 mt-3 first:mt-0">
                        {children}
                      </h2>
                    ),
                    h3: ({ children }) => (
                      <h3 className="text-sm md:text-base font-semibold mb-2 mt-3 first:mt-0">
                        {children}
                      </h3>
                    ),
                    h4: ({ children }) => (
                      <h4 className="text-sm md:text-base font-semibold mb-1 mt-2 first:mt-0">
                        {children}
                      </h4>
                    ),
                    h5: ({ children }) => (
                      <h5 className="text-xs md:text-sm font-semibold mb-1 mt-2 first:mt-0">
                        {children}
                      </h5>
                    ),
                    h6: ({ children }) => (
                      <h6 className="text-xs md:text-sm font-medium mb-1 mt-2 first:mt-0">
                        {children}
                      </h6>
                    ),
                    table: ({ children }) => (
                      <div className="overflow-x-auto my-3">
                        <table className="min-w-full border-collapse border border-border rounded-md">
                          {children}
                        </table>
                      </div>
                    ),
                    thead: ({ children }) => (
                      <thead className="bg-muted">
                        {children}
                      </thead>
                    ),
                    tbody: ({ children }) => (
                      <tbody>
                        {children}
                      </tbody>
                    ),
                    tr: ({ children }) => (
                      <tr className="border-b border-border">
                        {children}
                      </tr>
                    ),
                    th: ({ children }) => (
                      <th className="border border-border px-3 py-2 text-left text-xs md:text-sm font-semibold">
                        {children}
                      </th>
                    ),
                    td: ({ children }) => (
                      <td className="border border-border px-3 py-2 text-xs md:text-sm">
                        {children}
                      </td>
                    ),
                  }}
                >
                  {message.content}
                </ReactMarkdown>
              </div>
            ) : (
              <div className="whitespace-pre-wrap text-sm md:text-base leading-relaxed">
                {message.content}
              </div>
            )}
          </CardContent>

          {/* Message Actions (Show on hover for assistant messages) */}
          {isAssistant && (
            <div className="absolute -bottom-8 left-0 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
              <div className="flex items-center space-x-1">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleCopy}
                  className="h-7 px-2 text-xs"
                >
                  {copied ? (
                    <>
                      <CheckCheck className="mr-1 h-3 w-3" />
                      Copied
                    </>
                  ) : (
                    <>
                      <Copy className="mr-1 h-3 w-3" />
                      Copy
                    </>
                  )}
                </Button>
              </div>
            </div>
          )}
        </Card>

        {/* Message Metadata */}
        <div className={cn(
          "flex items-center mt-1 space-x-2 text-xs text-muted-foreground",
          isUser ? "flex-row-reverse space-x-reverse" : "flex-row"
        )}>
          {isUser && (
            <Badge variant="outline" className="text-xs px-1.5 py-0.5">
              You
            </Badge>
          )}
          {message.createdAt && (
            <span>{formatTime(message.createdAt)}</span>
          )}
        </div>
      </div>

      {/* User Avatar (Right side) */}
      {isUser && (
        <div className="flex-shrink-0">
          <Avatar className="h-8 w-8 border">
            <AvatarFallback className="bg-secondary text-secondary-foreground text-xs font-semibold">
              <User className="h-4 w-4" />
            </AvatarFallback>
          </Avatar>
        </div>
      )}
    </div>
  )
}

// Loading indicator component
export function ChatMessageLoading() {
  return (
    <div className="group flex w-full gap-3 px-3 py-4 md:px-4 justify-start">
      {/* Assistant Avatar */}
      <div className="flex-shrink-0">
        <Avatar className="h-8 w-8 border">
          <AvatarFallback className="bg-primary text-primary-foreground text-xs font-semibold">
            <Bot className="h-4 w-4" />
          </AvatarFallback>
        </Avatar>
      </div>

      {/* Loading Content */}
      <div className="flex max-w-[85%] md:max-w-[75%] flex-col items-start">
        <Card className="bg-card border-border">
          <CardContent className="p-3 md:p-4">
            <div className="flex items-center space-x-2">
              <div className="flex space-x-1">
                <div className="w-2 h-2 bg-gray-600 dark:bg-gray-100 rounded-full animate-bounce"></div>
                <div className="w-2 h-2 bg-gray-600 dark:bg-gray-100 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></div>
                <div className="w-2 h-2 bg-gray-600 dark:bg-gray-100 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
