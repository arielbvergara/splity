import { Header } from "@/components/header"
import { Hero } from "@/components/hero"
import { HowItWorks } from "@/components/how-it-works"

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-background">
      <Header />
      <Hero />
      <HowItWorks />
    </div>
  )
}
