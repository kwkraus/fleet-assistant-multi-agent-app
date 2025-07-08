import { ChatMessageLoading } from '../../components/chat/ChatMessage'

export default function TestLoading() {
  return (
    <div className="min-h-screen bg-background p-8">
      <h1 className="text-2xl font-bold mb-8">AI Thinking Animation Test</h1>
      <div className="space-y-4">
        <h2 className="text-lg font-semibold">New Loading Animation (with tooltip):</h2>
        <ChatMessageLoading />
        <p className="text-sm text-muted-foreground">
          Hover over the bouncing balls to see the &quot;Thinking...&quot; tooltip
        </p>
      </div>
    </div>
  )
}