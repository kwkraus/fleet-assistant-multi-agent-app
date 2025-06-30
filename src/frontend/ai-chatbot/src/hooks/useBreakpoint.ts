import { useState, useEffect } from 'react'
import { BREAKPOINTS } from '@/lib/constants'

type Breakpoint = keyof typeof BREAKPOINTS

export function useBreakpoint(breakpoint: Breakpoint): boolean {
  const [matches, setMatches] = useState(false)

  useEffect(() => {
    const mediaQuery = window.matchMedia(`(min-width: ${BREAKPOINTS[breakpoint]}px)`)
    
    const handleChange = (e: MediaQueryListEvent) => {
      setMatches(e.matches)
    }

    setMatches(mediaQuery.matches)
    mediaQuery.addEventListener('change', handleChange)
    
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [breakpoint])

  return matches
}

export function useCurrentBreakpoint(): Breakpoint {
  const [currentBreakpoint, setCurrentBreakpoint] = useState<Breakpoint>('sm')

  useEffect(() => {
    const updateBreakpoint = () => {
      const width = window.innerWidth
      
      if (width >= BREAKPOINTS['2xl']) {
        setCurrentBreakpoint('2xl')
      } else if (width >= BREAKPOINTS.xl) {
        setCurrentBreakpoint('xl')
      } else if (width >= BREAKPOINTS.lg) {
        setCurrentBreakpoint('lg')
      } else if (width >= BREAKPOINTS.md) {
        setCurrentBreakpoint('md')
      } else {
        setCurrentBreakpoint('sm')
      }
    }

    updateBreakpoint()
    window.addEventListener('resize', updateBreakpoint)
    
    return () => window.removeEventListener('resize', updateBreakpoint)
  }, [])

  return currentBreakpoint
}

// Specific breakpoint hooks
export const useIsMobile = () => !useBreakpoint('md')

export const useIsTablet = () => {
  const isMd = useBreakpoint('md')
  const isLg = useBreakpoint('lg')
  return isMd && !isLg
}

export const useIsDesktop = () => useBreakpoint('lg')
