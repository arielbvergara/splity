import { Scan, Users, DollarSign } from "lucide-react"

const steps = [
  {
    number: 1,
    title: "Scan Receipt",
    description: "Snap a photo of your receipt. Our AI extracts all items, prices, and totals instantly.",
    icon: Scan,
  },
  {
    number: 2,
    title: "Split Smart",
    description: "Choose how to split: evenly, by item, by percentage, or custom amounts.",
    icon: Users,
  },
  {
    number: 3,
    title: "Settle Up",
    description: "See who owes whom with simplified settlements. One-click payment tracking.",
    icon: DollarSign,
  },
]

export function HowItWorks() {
  return (
    <section className="bg-background py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Section Header */}
        <div className="text-center">
          <h2 className="text-balance text-4xl font-bold tracking-tight text-foreground sm:text-5xl">
            How Splity Works
          </h2>
          <p className="mt-4 text-pretty text-lg text-muted-foreground">
            Three simple steps to effortless expense sharing
          </p>
        </div>

        {/* Steps Grid */}
        <div className="mt-16 grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
          {steps.map((step) => {
            const Icon = step.icon
            return (
              <div
                key={step.number}
                className="group relative rounded-2xl border border-border bg-card p-8 transition-all hover:border-primary/50 hover:shadow-lg"
              >
                {/* Icon */}
                <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-xl bg-primary/10 transition-colors group-hover:bg-primary/20">
                  <Icon className="h-8 w-8 text-primary" />
                </div>

                {/* Content */}
                <h3 className="text-xl font-semibold text-foreground">
                  {step.number}. {step.title}
                </h3>
                <p className="mt-3 text-pretty leading-relaxed text-muted-foreground">{step.description}</p>
              </div>
            )
          })}
        </div>
      </div>
    </section>
  )
}
