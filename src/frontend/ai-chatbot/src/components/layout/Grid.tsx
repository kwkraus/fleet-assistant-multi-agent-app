import { cn } from "@/lib/utils"

interface GridProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode
  cols?: {
    default?: number
    sm?: number
    md?: number
    lg?: number
    xl?: number
  }
  gap?: "sm" | "md" | "lg" | "xl"
}

export function Grid({ 
  children, 
  className,
  cols = { default: 1 },
  gap = "md",
  ...props 
}: GridProps) {
  const gapClasses = {
    sm: "gap-2",
    md: "gap-4", 
    lg: "gap-6",
    xl: "gap-8"
  }

  const getColClasses = () => {
    const classes = ["grid"]
    
    if (cols.default) classes.push(`grid-cols-${cols.default}`)
    if (cols.sm) classes.push(`sm:grid-cols-${cols.sm}`)
    if (cols.md) classes.push(`md:grid-cols-${cols.md}`)
    if (cols.lg) classes.push(`lg:grid-cols-${cols.lg}`)
    if (cols.xl) classes.push(`xl:grid-cols-${cols.xl}`)
    
    return classes
  }

  return (
    <div 
      className={cn(
        ...getColClasses(),
        gapClasses[gap],
        className
      )} 
      {...props}
    >
      {children}
    </div>
  )
}

interface GridItemProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode
  span?: {
    default?: number
    sm?: number
    md?: number
    lg?: number
    xl?: number
  }
}

export function GridItem({ 
  children, 
  className,
  span = { default: 1 },
  ...props 
}: GridItemProps) {
  const getSpanClasses = () => {
    const classes = []
    
    if (span.default) classes.push(`col-span-${span.default}`)
    if (span.sm) classes.push(`sm:col-span-${span.sm}`)
    if (span.md) classes.push(`md:col-span-${span.md}`)
    if (span.lg) classes.push(`lg:col-span-${span.lg}`)
    if (span.xl) classes.push(`xl:col-span-${span.xl}`)
    
    return classes
  }

  return (
    <div 
      className={cn(...getSpanClasses(), className)} 
      {...props}
    >
      {children}
    </div>
  )
}
