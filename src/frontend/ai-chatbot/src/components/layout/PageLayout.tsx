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
    <div className={cn("h-full w-full flex flex-col overflow-hidden", className)}>
      {header && (
        <header className="flex-shrink-0 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          {header}
        </header>
      )}
      
      <main className="flex-1 min-h-0 overflow-hidden">
        {children}
      </main>
      
      {footer && (
        <footer className="flex-shrink-0 border-t bg-background">
          {footer}
        </footer>
      )}
    </div>
  )
}
