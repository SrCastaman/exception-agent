# ExceptionAgent

> **AI-powered operational exception analysis for procurement and supply chain, using deterministic business logic and a local LLM.**

**Status:** Prototype / portfolio project

ExceptionAgent is a .NET application designed to help procurement, supply chain and operations teams investigate supplier-related operational exceptions such as delayed purchase orders.

Instead of simply detecting that a purchase order is late, ExceptionAgent combines operational data from purchase orders, inventory and customer demand to determine **what the delay actually affects, how much impact it creates, and why**.

A local LLM, **Qwen3:8b running through Ollama**, then turns the deterministic analysis into a structured diagnosis and human-readable explanation.

---

## The problem

A supplier sends an email:

> "PO-1042 will arrive on August 20 instead of August 15."

Knowing that the PO is late is easy. The difficult part is understanding the consequences:

- Which products are affected?
- How many units are still missing?
- Which customer orders depend on that supply?
- Will those orders actually become late?
- Is there enough stock or another purchase order to cover the demand?
- How much of the final shortage is really attributable to this specific supplier delay?

In a real operation, answering those questions can require checking emails, purchase orders, inventory, customer orders and delivery dates manually.

**ExceptionAgent automates that investigation.**

---

## What it does

The current prototype follows this flow:

```text
Supplier email
      ↓
Email extraction / matching
      ↓
Operational exception
      ↓
Deterministic impact analysis (C#)
      ↓
Supply / demand allocation
      ↓
Scenario comparison
      ↓
InvestigationContext
      ↓
Qwen 3 via Ollama
      ↓
Structured diagnosis
      ↓
Web UI
```

The key architectural principle is that **the LLM is not responsible for critical business calculations**.

C# determines the operational facts using deterministic business logic. The LLM receives those already-calculated facts and is responsible for interpreting, summarizing and communicating them.

This separation is intended to reduce the risk of hallucinated quantities, dates or shortages while keeping the AI layer replaceable.

---

---

## Screenshots

### Exception Dashboard

The main dashboard provides an overview of the operational exceptions detected by the system, including their severity, status and associated purchase orders.

![Exception Dashboard](docs/images/screenshot1.png)

### Exception Details

The exception details view provides the operational context behind a detected problem, including the purchase order, supplier, dates and quantities involved.

![Exception Details](docs/images/screenshot2.png)

### Impact Analysis

This view shows the deterministic impact analysis, including affected customer demand and the consequences of the detected exception.

![Impact Analysis](docs/images/screenshot3.png)

### AI Investigation

Qwen3 receives the structured investigation context and generates a diagnosis explaining the likely cause, impact and possible actions.

![AI Investigation](docs/images/screenshot4.png)

## Core architecture

### 1. Email ingestion and matching

Supplier emails are extracted and associated with operational entities such as suppliers and purchase orders.

The application separates email ingestion/matching from the later exception investigation workflow.

### 2. Exception detection

The system identifies operational problems such as delayed purchase orders and creates an `OperationalException` describing the detected issue.

### 3. Deterministic Impact and Risk Calculation

Before calling the LLM, the application calculates the operational impact using application code.

This includes:

- purchase order quantities;
- received vs. pending quantities;
- expected/current availability dates;
- available inventory;
- customer demand;
- required dates;
- supply-to-demand allocation;
- uncovered demand;
- affected customer orders;
- risk dates;
- severity and impact.

### 4. Supply allocation engine

The allocation engine answers a fundamental operational question:

> **Given the supply available on each date, which customer demands can actually be covered?**

The current prototype uses a deterministic **date-priority allocation policy**, meaning customer demand is evaluated according to required delivery date and only supply available in time can satisfy that demand.
```text
Supply
├── STOCK
├── PO-1042
└── PO-1044

Demand
├── CO-8823
├── CO-8821
└── CO-8824

        ↓
Date-priority allocation
        ↓
Allocations + uncovered demand
```

### 5. Scenario analysis

The system does not only calculate the current situation. It can compare scenarios to estimate the **marginal impact of a specific exception**.

For example:

```text
Scenario A → normal supply
Scenario B → PO-1042 delayed
Scenario C → PO-1044 delayed
Scenario D → both delayed
```

This allows the application to distinguish between:

> "The supply chain currently has a shortage"

and:

> "This specific purchase-order delay is responsible for X additional units of impact."

That distinction is central to the project.

---

## AI layer

### Model

The current prototype uses:

- **Qwen3:8b**
- **Ollama**
- Local HTTP endpoint: `http://localhost:11434`

This keeps the prototype independent from paid hosted LLM APIs.

### What the AI receives

Qwen receives an `InvestigationContext` containing already-calculated operational facts, rather than raw business data that it would have to calculate itself.

Conceptually:

```text
Purchase order
Current / expected dates
Inventory
Customer orders
Supply allocation
Uncovered demand
Affected orders
Risk date
Severity / impact
Evidence from supplier communication
```

### What the AI does

The LLM is responsible for interpretation and communication, including:

- identifying the likely cause;
- summarizing what happened;
- explaining the operational impact;
- identifying affected orders from the supplied facts;
- suggesting possible next actions;
- returning structured JSON;
- providing confidence and evidence.

The intended separation is:

```text
C#  → operational truth
Qwen → interpretation and explanation
```

This reduces the risk of an LLM inventing quantities, dates or shortages that should instead come from deterministic application logic.

---

## Example

Suppose the system knows:

```text
PO-1042
Original expected date: 15/08/2026
New expected date:      20/08/2026

Customer orders:
CO-8823 → 25 units → required 18/08
CO-8821 → 25 units → required 19/08

Available stock: 5 units
```

The deterministic layer calculates the allocation and impact first.

The LLM can then turn that context into a diagnosis such as:

```text
Severity: HIGH
Cause: supplier delay
Affected orders: CO-8823, CO-8821
Impact: 50 units
Risk date: 18/08/2026
```

The important point is that the model is **explaining a result produced by the application**, not deciding the arithmetic itself.

---

## Reliability and failure handling

The application also protects the main workflow from slow or unavailable local inference.

The HTTP client used by the AI service has a timeout, and cancellation is propagated through the analysis flow.

If the local model does not respond in time:

```text
Ollama / Qwen timeout
        ↓
Analysis is cancelled
        ↓
AgentService handles the failure
        ↓
Application remains responsive
```

During development this behavior was explicitly tested with an artificially short timeout.

In normal local inference, Qwen3:8b has been observed responding in roughly the 10–15 second range for the current prompts and hardware.

---

## Testing

The project uses **xUnit** for automated tests.

The test suite covers the allocation and exception-analysis logic, including scenarios such as:

- delayed supply with an actual shortage;
- delayed supply with no customer impact;
- multiple delayed purchase orders;
- partial delivery;
- alternative supply covering demand;
- scenario-based impact calculation;
- allocation behavior;
- integration between scenario generation and impact calculation.

The current development benchmark contains **16 passing tests**.

The tests are intentionally focused on deterministic business logic so that changes to the allocation and impact engines can be validated independently from LLM output.

---

## Technology stack

| Area | Technology |
|---|---|
| Backend | C# / .NET 10 |
| Web framework | ASP.NET Core / Razor Pages |
| ORM | Entity Framework Core |
| Database | SQLite |
| AI runtime | Ollama |
| LLM | Qwen3:8b |
| AI communication | HTTP / local Ollama API |
| Testing | xUnit |
| Version control | Git / GitHub |

---

## Project structure

The project is organized around application responsibilities rather than putting all logic into the web layer.

```text
ExceptionAgent/
├── Application/
│   ├── Allocation/
│   │   ├── Models/
│   │   ├── Policies/
│   │   ├── AllocationEngine.cs
│   │   ├── AllocationDataService.cs
│   │   ├── AllocationImpactService.cs
│   │   ├── AllocationScenarioBuilder.cs
│   │   ├── AllocationScenarioService.cs
│   │   └── ScenarioImpactCalculator.cs
│   ├── Email/
│   └── Exceptions/
├── Contracts/
├── Data/
├── Domain/
│   ├── Entities/
│   └── Enums/
├── Infrastructure/
│   └── AI/
├── Migrations/
├── Pages/
└── Program.cs

ExceptionAgent.Tests/
├── AllocationTest/
└── Evaluation/
```

The repository also contains the solution file at the root:

```text
ExceptionAgent.slnx
```

---

## Getting started

### Prerequisites

Install:

- .NET 10 SDK
- Ollama
- Qwen3:8b model

Pull the local model with Ollama:

```bash
ollama pull qwen3:8b
```

Start Ollama if it is not already running.

### Clone the repository

```bash
git clone https://github.com/SrCastaman/exception-agent.git
cd exception-agent
```

### Build

```bash
dotnet build .\ExceptionAgent.slnx
```

### Run tests

```bash
dotnet test .\ExceptionAgent.Tests\ExceptionAgent.Tests.csproj
```

### Run the application

```bash
dotnet run --project .\ExceptionAgent\ExceptionAgent.csproj
```

The application runs locally and communicates with Ollama through its local HTTP API.

---

## Current scope

ExceptionAgent is currently a **prototype / portfolio project**, not a production-ready enterprise platform.

The current focus is deliberately narrow:

> **Understand supplier-related operational exceptions and quantify their real impact.**

The project has explored future directions such as:

- Microsoft Outlook integration;
- ERP integrations;
- company-specific policies and procedures;
- document-based policy retrieval;
- configurable workflows;
- learning from historical human decisions.

Those are intentionally not treated as solved problems yet.

---

## Why the architecture is split between deterministic code and AI

A general-purpose LLM is good at interpreting unstructured information, but business-critical quantities and dates should be reproducible.

For example, the system should not ask an LLM to decide whether:

```text
25 units + 25 units = 50
```

or whether a supply arriving on `20/08` can satisfy a customer requirement on `18/08`.

Those decisions belong to the deterministic application layer.

The LLM becomes useful after those facts have been established:

```text
Database + business rules
        ↓
Reliable operational facts
        ↓
LLM
        ↓
Human-readable diagnosis
```

This architecture also makes it possible to change the LLM later without rewriting the core business logic.

---

## Project goals

The long-term idea behind ExceptionAgent is to become a **copilot for operational exceptions in procurement and supply chain**:

```text
Detect
  ↓
Investigate
  ↓
Quantify impact
  ↓
Explain
  ↓
Recommend
  ↓
Eventually automate / learn from outcomes
```

The current prototype focuses on proving the first four steps reliably before moving toward deeper integrations and automation.

---

## Status

**Prototype — core exception investigation implemented.**

The project currently demonstrates:

- supplier email → exception workflow;
- deterministic supply/demand allocation;
- scenario-based impact attribution;
- structured investigation context;
- local LLM analysis with Qwen3:8b;
- timeout/cancellation handling;
- automated tests for the deterministic core.

---

## Author

**Rafael Castañeda**

GitHub: [@SrCastaman](https://github.com/SrCastaman)
