// App constants
export const APP_NAME = "Fleet Assistant"
export const APP_DESCRIPTION = "Your AI-powered fleet management assistant"

// UI Constants
export const BREAKPOINTS = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  '2xl': 1536,
} as const

export const MESSAGE_ROLES = {
  USER: 'user',
  ASSISTANT: 'assistant',
  SYSTEM: 'system',
} as const

export const ANIMATION_DURATION = {
  fast: 150,
  normal: 200,
  slow: 300,
} as const

// Quick action prompts
export const QUICK_PROMPTS = [
  {
    title: "Maintenance Schedule",
    prompt: "What are the current maintenance schedules for my fleet?"
  },
  {
    title: "Fuel Reports", 
    prompt: "Show me fuel efficiency reports for this month"
  },
  {
    title: "Route Planning",
    prompt: "Help me plan optimal routes for my deliveries"
  },
  {
    title: "Safety Compliance",
    prompt: "Review safety compliance status for all vehicles"
  },
  {
    title: "Cost Analysis",
    prompt: "Provide a cost analysis breakdown for this quarter"
  }
] as const

// Theme configuration
export const THEME = {
  colors: {
    primary: {
      50: '#eff6ff',
      500: '#3b82f6',
      600: '#2563eb',
      900: '#1e3a8a',
    },
    gray: {
      50: '#f9fafb',
      100: '#f3f4f6',
      500: '#6b7280',
      900: '#111827',
    }
  }
} as const
