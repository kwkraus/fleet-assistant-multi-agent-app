# Fleet Assistant Multi-Agent App Constitution

## Core Principles

### I. Code Quality Discipline (NON-NEGOTIABLE)
- MUST preserve the repository pattern and shared model contracts defined in `FleetAssistant.Shared`; breaking changes require coordinated updates across WebAPI, agents, and frontend.
- MUST keep correlation ID logging and structured telemetry in every controller, background process, and integration surface, following `copilot-instructions.md` guidance on observability.
- SHOULD prefer small, reviewable commits with updated docs in `docs/` whenever behaviors, blueprints, or agent wiring change.

### II. Test-First Assurance (NON-NEGOTIABLE)
- MUST add or update automated coverage before merging any change that affects agent coordination, chat streaming, repositories, or shared DTOs.
- MUST run the relevant scripts in `testing/` (Node + PowerShell) for features that touch the chat pipeline, Foundry integration, or data persistence; failures block release until green.
- SHOULD use the in-memory EF Core provider for unit tests and the full integration harness for regression-sensitive features (e.g., multi-agent orchestration, blob ingestion).

### III. Conversation UX Consistency (NON-NEGOTIABLE)
- MUST maintain the SSE streaming contract (`text/event-stream`, `data:` payloads) between WebAPI and frontend; any deviation requires simultaneous backend/frontend updates and regression tests.
- MUST keep the Next.js chat interface aligned with the documented Tailwind and Radix component patterns, ensuring consistent states, accessibility, and message ordering.
- SHOULD deliver UX changes through design tokens or shared utilities so that tenant-specific branding never breaks the default experience.

### IV. Performance & Resilience Standards (NON-NEGOTIABLE)
- MUST preserve streaming responsiveness by avoiding blocking calls in controller pipelines; background operations belong in dedicated services or async workflows.
- MUST safeguard concurrent conversations by respecting the `FoundryAgentService` thread mapping and using thread-safe primitives when extending agent state.
- SHOULD baseline endpoints with Application Insights metrics; regressions in latency, memory, or throughput trigger a performance review before release.

## Quality Gates & Metrics
- Every PR MUST document which principles were touched and how compliance was verified (tests run, telemetry reviewed, UX snapshots, etc.).
- The `/speckit.plan` and `/speckit.tasks` outputs MUST reference constitution guards whenever planning work that risks violating a principle.
- Release candidates SHOULD include a summarized log of Key Performance Indicators (latency, error rate, streaming completion) captured from the latest integration run.

## Development Workflow Expectations
- Spec, plan, and implementation artifacts MUST cross-link to the relevant sections of `copilot-instructions.md` when introducing new agents, data flows, or UI components.
- Code reviews MUST explicitly check correlation IDs, test coverage evidence, UX accessibility, and streaming performance notes before approval.
- When adding Azure or external service dependencies, engineers SHOULD update checklists and runbooks so the multi-agent team can operate the feature without drift.

## Governance
- The constitution supersedes ad hoc conventions; exceptions demand a written amendment proposal with risk analysis and migration steps.
- Enforcement occurs during `/speckit.analyze`, code review, and integration testing; violations block merges until resolved or formally waived through governance.
- Guidance updates to `copilot-instructions.md`, architecture docs, or testing scripts MUST be reflected here during the same change cycle.

**Version**: 1.0.0 | **Ratified**: 2025-10-07 | **Last Amended**: 2025-10-07