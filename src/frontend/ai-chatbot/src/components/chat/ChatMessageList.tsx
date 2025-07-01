"use client"

import { useEffect, useRef } from "react"
import { MessageCircle, Zap, Route, Shield, DollarSign } from "lucide-react"
import { ChatMessage, ChatMessageLoading } from "./ChatMessage"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Card, CardContent } from "@/components/ui/card"
import { Container } from "@/components/layout/Container"
import { Grid, GridItem } from "@/components/layout/Grid"
import { APP_NAME } from "@/lib/constants"
import { cn } from "@/lib/utils"

interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  createdAt?: Date
}

interface ChatMessageListProps {
  messages: ChatMessage[]
  isLoading: boolean
  className?: string
}

export function ChatMessageList({ messages, isLoading, className }: ChatMessageListProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    scrollToBottom()
  }, [messages, isLoading])

  if (messages.length === 0) {
    return <ChatWelcomeScreen />
  }

  return (
    <div className={cn("flex-1 overflow-hidden", className)}>
      <ScrollArea className="h-full chat-scroll">
        <Container size="lg">
          <div className="py-4 md:py-6">
            {messages.map((message, index) => (
              <ChatMessage
                key={message.id}
                message={message}
                isLastMessage={index === messages.length - 1}
              />
            ))}
            
            {isLoading && <ChatMessageLoading />}
            
            {/* Scroll anchor */}
            <div ref={messagesEndRef} className="h-1" />
          </div>
        </Container>
      </ScrollArea>
    </div>
  )
}

function ChatWelcomeScreen() {
  const features = [
    {
      icon: Zap,
      title: "Maintenance Schedules",
      description: "Track and optimize vehicle maintenance timing"
    },
    {
      icon: Route,
      title: "Route Planning", 
      description: "Optimize delivery routes and logistics"
    },
    {
      icon: Shield,
      title: "Safety Compliance",
      description: "Monitor safety protocols and compliance"
    },
    {
      icon: DollarSign,
      title: "Cost Analysis",
      description: "Analyze fleet costs and efficiency metrics"
    }
  ]

  return (
    <div className="flex-1 overflow-auto">
      <Container size="md">
        <div className="flex flex-col items-center justify-center min-h-[60vh] py-8 md:py-12">
          {/* Welcome Header */}
          <div className="text-center mb-8 md:mb-12">
            <div className="flex items-center justify-center w-16 h-16 md:w-20 md:h-20 bg-primary rounded-2xl mb-4 md:mb-6 mx-auto">
              <MessageCircle className="h-8 w-8 md:h-10 md:w-10 text-primary-foreground" />
            </div>
            <h1 className="text-2xl md:text-3xl font-bold text-foreground mb-2 md:mb-3">
              Welcome to {APP_NAME}
            </h1>
            <p className="text-sm md:text-base text-muted-foreground max-w-md mx-auto leading-relaxed">
              Your AI-powered fleet management assistant. I&apos;m here to help you optimize 
              operations, track maintenance, and improve efficiency.
            </p>
          </div>

          {/* Feature Grid */}
          <div className="w-full mb-8 md:mb-12">
            <h2 className="text-lg md:text-xl font-semibold text-center mb-4 md:mb-6">
              What can I help you with?
            </h2>
            <Grid 
              cols={{ 
                default: 1, 
                sm: 2, 
                lg: 4 
              }} 
              gap="lg"
            >
              {features.map((feature, index) => (
                <GridItem key={index}>
                  <Card className="h-full transition-all duration-200 hover:shadow-md hover:border-primary/50 cursor-pointer group">
                    <CardContent className="p-4 md:p-6 text-center">
                      <div className="flex items-center justify-center w-12 h-12 bg-primary/10 rounded-lg mb-3 md:mb-4 mx-auto group-hover:bg-primary/20 transition-colors">
                        <feature.icon className="h-6 w-6 text-primary" />
                      </div>
                      <h3 className="font-semibold text-sm md:text-base mb-2">
                        {feature.title}
                      </h3>
                      <p className="text-xs md:text-sm text-muted-foreground leading-relaxed">
                        {feature.description}
                      </p>
                    </CardContent>
                  </Card>
                </GridItem>
              ))}
            </Grid>
          </div>

          {/* Quick Start Suggestions */}
          <div className="text-center">
            <p className="text-sm text-muted-foreground mb-4">
              Try asking me something like:
            </p>
            <div className="space-y-2">
              <p className="text-sm font-medium text-foreground">
                &quot;What maintenance is due for my fleet this week?&quot;
              </p>
              <p className="text-sm font-medium text-foreground">
                &quot;Show me the most fuel-efficient routes&quot;
              </p>
              <p className="text-sm font-medium text-foreground">
                &quot;Generate a safety compliance report&quot;
              </p>
            </div>
          </div>
        </div>
      </Container>
    </div>
  )
}
