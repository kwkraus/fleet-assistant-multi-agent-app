import { cn } from "@/lib/utils"

interface PageLayoutProps {
  children: React.ReactNode
  className?: string
  header?: React.ReactNode
  footer?: React.ReactNode
}

export function PageLayout({ 
  children, 
  className,
  header,
  footer 
}: PageLayoutProps) {
  return (
    <div className={cn("h-screen flex flex-col", className)}>
      {header && (
        <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 flex-shrink-0">
          {header}
        </header>
      )}
      
      <main className="flex-1 min-h-0 flex flex-col">
        {children}
      </main>
      
      {footer && (
        <footer className="border-t bg-background flex-shrink-0">
          {footer}
        </footer>
      )}
    </div>
  )
}
