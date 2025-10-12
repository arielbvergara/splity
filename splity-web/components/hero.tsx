import { Button } from "@/components/ui/button"
import { ArrowRight, Scan, Users, Zap } from "lucide-react"
import Link from "next/link"

export function Hero() {
  return (
    <section className="relative min-h-[600px] overflow-hidden">
      {/* Background Image with Overlay */}
      <div className="absolute inset-0 z-0">
        <img src="/delicious-gourmet-burger-sandwich-with-fresh-ingre.jpg" alt="Food background" className="h-full w-full object-cover" />
        <div className="absolute inset-0 bg-black/60" />
      </div>

      {/* Content */}
      <div className="relative z-10 mx-auto max-w-7xl px-4 py-24 sm:px-6 lg:px-8">
        <div className="max-w-3xl">
          <h1 className="text-balance text-5xl font-bold tracking-tight text-white sm:text-6xl lg:text-7xl">
            Split Bills Instantly with AI-Powered Receipt Scanning
          </h1>

          <p className="mt-6 text-pretty text-lg leading-relaxed text-white/90 sm:text-xl">
            Stop the spreadsheet chaos. Splity automates expense sharing with smart receipt scanning, fair splitting,
            and instant settlements.
          </p>

          {/* CTAs */}
          <div className="mt-10 flex flex-wrap gap-4">
            <Button size="lg" className="gap-2" asChild>
              <Link href="/dashboard">
                Get Started <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
            <Button size="lg" variant="outline" className="border-white/20 bg-white/10 text-white hover:bg-white/20">
              See How It Works
            </Button>
          </div>

          {/* Feature Highlights */}
          <div className="mt-16 grid gap-6 sm:grid-cols-3">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/20 backdrop-blur-sm">
                <Scan className="h-5 w-5 text-white" />
              </div>
              <div>
                <h3 className="font-semibold text-white">AI Receipt Scan</h3>
                <p className="text-sm text-white/80">Instant extraction</p>
              </div>
            </div>

            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/20 backdrop-blur-sm">
                <Users className="h-5 w-5 text-white" />
              </div>
              <div>
                <h3 className="font-semibold text-white">Smart Splitting</h3>
                <p className="text-sm text-white/80">Fair & flexible</p>
              </div>
            </div>

            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/20 backdrop-blur-sm">
                <Zap className="h-5 w-5 text-white" />
              </div>
              <div>
                <h3 className="font-semibold text-white">Quick Settle</h3>
                <p className="text-sm text-white/80">Who owes whom</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
