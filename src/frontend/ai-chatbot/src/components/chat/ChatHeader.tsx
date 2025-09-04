"use client"

import { MessageSquare, Trash2, MoreVertical } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { ThemeSelector } from "@/components/theme-selector"
import { Container } from "@/components/layout/Container"
import { NotificationBell } from "@/components/notifications"
import { APP_NAME, APP_DESCRIPTION } from "@/lib/constants"
import { cn } from "@/lib/utils"

interface ChatHeaderProps {
  conversationId?: string | null
  onClearConversation: () => void
  className?: string
}

export function ChatHeader({ 
  conversationId, 
  onClearConversation, 
  className 
}: ChatHeaderProps) {
  return (
    <header className={cn(
      "sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60",
      className
    )}>
      <Container size="lg">
        <div className="flex items-center justify-between py-3 md:py-4">
          {/* Logo and Title */}
          <div className="flex items-center space-x-3">
            <div className="flex items-center justify-center w-8 h-8 md:w-10 md:h-10 bg-primary rounded-lg">
              <MessageSquare className="h-4 w-4 md:h-5 md:w-5 text-primary-foreground" />
            </div>
            <div className="hidden sm:block">
              <h1 className="text-lg md:text-xl font-semibold text-foreground">
                {APP_NAME}
              </h1>
              <p className="text-xs md:text-sm text-muted-foreground hidden md:block">
                {APP_DESCRIPTION}
              </p>
            </div>
            <div className="sm:hidden">
              <h1 className="text-lg font-semibold text-foreground">
                Fleet Assistant
              </h1>
            </div>
          </div>

          {/* Status and Actions */}
          <div className="flex items-center space-x-2 md:space-x-3">
            {/* Conversation Status */}
            {conversationId && (
              <Badge variant="secondary" className="hidden md:flex">
                <div className="w-2 h-2 bg-green-500 rounded-full mr-2" />
                Active Session
              </Badge>
            )}

            {/* Mobile Actions Menu */}
            <div className="flex md:hidden items-center space-x-2">
              <NotificationBell />
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <MoreVertical className="h-4 w-4" />
                    <span className="sr-only">Open menu</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="min-w-[200px]">
                  {conversationId && (
                    <>
                      <DropdownMenuItem>
                        <div className="flex items-center">
                          <div className="w-2 h-2 bg-green-500 rounded-full mr-2" />
                          Session: {conversationId.substring(0, 8)}...
                        </div>
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem 
                        onClick={onClearConversation}
                        className="text-destructive focus:text-destructive"
                      >
                        <Trash2 className="mr-2 h-4 w-4" />
                        Clear Conversation
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                    </>
                  )}
                  <DropdownMenuItem asChild>
                    <div className="flex items-center justify-between w-full">
                      <span>Theme</span>
                      <ThemeSelector />
                    </div>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            {/* Desktop Actions */}
            <div className="hidden md:flex items-center space-x-2">
              {conversationId && (
                <div className="flex items-center space-x-2">
                  <Badge variant="outline" className="text-xs">
                    {conversationId.substring(0, 8)}...
                  </Badge>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={onClearConversation}
                    className="text-muted-foreground hover:text-destructive"
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Clear
                  </Button>
                </div>
              )}
              <NotificationBell />
              <ThemeSelector />
            </div>
          </div>
        </div>
      </Container>
    </header>
  )
}
