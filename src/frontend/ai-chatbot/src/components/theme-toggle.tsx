"use client"

import * as React from "react"
import { Moon, Sun } from "lucide-react"

import { Button } from "@/components/ui/button"
import { useTheme } from "@/components/theme-provider"
import { cn } from "@/lib/utils"

interface ThemeToggleProps {
  className?: string
  size?: "sm" | "default" | "lg"
}

export function ThemeToggle({ className, size = "default" }: ThemeToggleProps) {
  const { theme, setTheme } = useTheme()

  const cycleTheme = () => {
    setTheme(theme === "light" ? "dark" : "light")
  }

  const getIcon = () => {
    return theme === "light" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />
  }

  const getLabel = () => {
    return `Switch to ${theme === "light" ? "dark" : "light"} mode`
  }

  return (
    <Button
      variant="ghost"
      size={size === "sm" ? "sm" : size === "lg" ? "lg" : "icon"}
      onClick={cycleTheme}
      className={cn("transition-colors", className)}
      aria-label={getLabel()}
      title={getLabel()}
    >
      {getIcon()}
      {size !== "default" && (
        <span className="ml-2 capitalize">{theme}</span>
      )}
    </Button>
  )
}
