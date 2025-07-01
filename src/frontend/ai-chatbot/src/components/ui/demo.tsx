import * as React from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Textarea } from "@/components/ui/textarea"
import { Separator } from "@/components/ui/separator"

export function ShadcnDemo() {
  return (
    <div className="p-8 space-y-6 max-w-md mx-auto">
      <Card>
        <CardHeader>
          <CardTitle>Shadcn UI Demo</CardTitle>
          <CardDescription>Testing our Phase 1 setup</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center space-x-4">
            <Avatar>
              <AvatarImage src="https://github.com/shadcn.png" />
              <AvatarFallback>AI</AvatarFallback>
            </Avatar>
            <div>
              <p className="text-sm font-medium">Fleet Assistant</p>
              <p className="text-xs text-muted-foreground">AI Assistant</p>
            </div>
          </div>
          
          <Separator />
          
          <Textarea placeholder="Type your message here..." />
          
          <div className="flex gap-2">
            <Button variant="default">Send</Button>
            <Button variant="outline">Clear</Button>
            <Button variant="ghost">Cancel</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
