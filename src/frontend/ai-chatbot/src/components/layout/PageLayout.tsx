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
        <header className="w-full border-b border-gray-200 bg-gray-100 dark:border-gray-700 dark:bg-gray-800 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          {header}
        </header>
      )}
      
      <main className="overflow-hidden bg-white dark:bg-neutral-900">
        {children}
      </main>
      
      {footer && (
        <footer className="border-t border-gray-200 bg-gray-100 dark:border-gray-700 dark:bg-gray-800">
          {footer}
        </footer>
      )}
    </div>
  )
}
