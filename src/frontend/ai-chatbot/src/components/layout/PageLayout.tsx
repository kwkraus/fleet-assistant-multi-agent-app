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
    <div className={cn("h-full w-full grid grid-rows-[auto_1fr_auto] overflow-hidden", className)}>
      {header && (
        <header className="w-full border-b border-border/70 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          {header}
        </header>
      )}
      
      <main className="overflow-hidden">
        {children}
      </main>
      
      {footer && (
        <footer className="border-t border-border/70 bg-background">
          {footer}
        </footer>
      )}
    </div>
  )
}
