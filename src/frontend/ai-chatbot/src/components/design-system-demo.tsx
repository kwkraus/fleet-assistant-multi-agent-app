"use client"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Separator } from "@/components/ui/separator"
import { ThemeSelector } from "@/components/theme-selector"
import { Container } from "@/components/layout/Container"
import { Grid, GridItem } from "@/components/layout/Grid"
import { PageLayout } from "@/components/layout/PageLayout"
import { useIsMobile, useIsDesktop } from "@/hooks/useBreakpoint"
import { THEME } from "@/lib/constants"

export function DesignSystemDemo() {
  const isMobile = useIsMobile()
  const isDesktop = useIsDesktop()

  const header = (
    <Container>
      <div className="flex items-center justify-between py-4">
        <div className="flex items-center space-x-4">
          <h1 className="text-xl font-semibold">Design System Demo</h1>
          <span className="text-sm text-muted-foreground">
            {isMobile ? "Mobile" : isDesktop ? "Desktop" : "Tablet"} View
          </span>
        </div>
        <ThemeSelector />
      </div>
    </Container>
  )

  return (
    <PageLayout header={header}>
      <Container className="py-8">
        <div className="space-y-8">
          {/* Colors Section */}
          <Card>
            <CardHeader>
              <CardTitle>Color Palette</CardTitle>
              <CardDescription>
                Primary and secondary color scales with semantic variants
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Grid cols={{ default: 1, md: 2 }} gap="lg">
                <GridItem>
                  <h4 className="mb-4 font-medium">Primary Colors</h4>
                  <div className="grid grid-cols-5 gap-2">
                    {Object.entries(THEME.colors.primary).map(([shade, color]) => (
                      <div key={shade} className="text-center">
                        <div 
                          className="h-12 w-full rounded border mb-2"
                          style={{ backgroundColor: color }}
                        />
                        <div className="text-xs text-muted-foreground">{shade}</div>
                      </div>
                    ))}
                  </div>
                </GridItem>
                <GridItem>
                  <h4 className="mb-4 font-medium">Secondary Colors</h4>
                  <div className="grid grid-cols-5 gap-2">
                    {Object.entries(THEME.colors.secondary).slice(0, 5).map(([shade, color]) => (
                      <div key={shade} className="text-center">
                        <div 
                          className="h-12 w-full rounded border mb-2"
                          style={{ backgroundColor: color }}
                        />
                        <div className="text-xs text-muted-foreground">{shade}</div>
                      </div>
                    ))}
                  </div>
                </GridItem>
              </Grid>
            </CardContent>
          </Card>

          {/* Typography Section */}
          <Card>
            <CardHeader>
              <CardTitle>Typography</CardTitle>
              <CardDescription>
                Font scales and text treatments
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="text-4xl font-bold">Heading 1</div>
              <div className="text-3xl font-semibold">Heading 2</div>
              <div className="text-2xl font-medium">Heading 3</div>
              <div className="text-xl">Heading 4</div>
              <div className="text-lg">Heading 5</div>
              <Separator />
              <div className="text-base">
                This is body text in the base size. It&apos;s designed to be highly readable 
                and comfortable for extended reading sessions.
              </div>
              <div className="text-sm text-muted-foreground">
                This is smaller text, often used for captions, labels, or secondary information.
              </div>
            </CardContent>
          </Card>

          {/* Components Section */}
          <Card>
            <CardHeader>
              <CardTitle>UI Components</CardTitle>
              <CardDescription>
                Various UI components from our design system
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Grid cols={{ default: 1, md: 2, lg: 3 }} gap="lg">
                <GridItem>
                  <div className="space-y-4">
                    <h4 className="font-medium">Buttons</h4>
                    <div className="space-y-2">
                      <Button className="w-full">Primary Button</Button>
                      <Button variant="secondary" className="w-full">Secondary</Button>
                      <Button variant="outline" className="w-full">Outline</Button>
                      <Button variant="ghost" className="w-full">Ghost</Button>
                    </div>
                  </div>
                </GridItem>
                
                <GridItem>
                  <div className="space-y-4">
                    <h4 className="font-medium">Avatars</h4>
                    <div className="flex space-x-2">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback>U</AvatarFallback>
                      </Avatar>
                      <Avatar className="h-10 w-10">
                        <AvatarFallback>AI</AvatarFallback>
                      </Avatar>
                      <Avatar className="h-12 w-12">
                        <AvatarFallback>FA</AvatarFallback>
                      </Avatar>
                    </div>
                  </div>
                </GridItem>

                <GridItem>
                  <div className="space-y-4">
                    <h4 className="font-medium">Cards</h4>
                    <Card className="p-4">
                      <div className="text-sm font-medium">Sample Card</div>
                      <div className="text-xs text-muted-foreground mt-1">
                        This is a sample card component
                      </div>
                    </Card>
                  </div>
                </GridItem>
              </Grid>
            </CardContent>
          </Card>

          {/* Spacing Section */}
          <Card>
            <CardHeader>
              <CardTitle>Spacing System</CardTitle>
              <CardDescription>
                Consistent spacing scale used throughout the application
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {Object.entries(THEME.spacing).map(([name, value]) => (
                  <div key={name} className="flex items-center space-x-4">
                    <div className="w-16 text-sm font-mono">{name}</div>
                    <div className="w-20 text-sm text-muted-foreground">{value}</div>
                    <div 
                      className="bg-primary h-4 rounded"
                      style={{ width: value }}
                    />
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </Container>
    </PageLayout>
  )
}
