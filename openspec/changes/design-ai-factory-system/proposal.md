# Proposal: AI Software Factory System

## Problem Statement

Software delivery teams lack automated instrumentation of their end-to-end delivery process. Work items move through statuses — from conceptual definition through design, development, review, testing, deployment, and production stabilization — without unified tracking, automated responses to status changes, or consolidated metrics. Teams cannot easily measure throughput, detect implicit defects (backward status transitions), or identify stuck work items. Additionally, there is no standardized way to plug in automated worker agents alongside human contributors at each stage of the delivery pipeline.

## Proposed Solution

Introduce an AI Software Factory system composed of three modules:

1. **Work Item Instrumentation** — Tracks every work item through its lifecycle statuses, records status entry/exit timestamps, integrates with external work tracking systems via webhooks, and translates webhook payloads into internal factory events. Produces reports on throughput, implicit defects, and stuck items, segmented by work item type.

2. **Worker Agents** — Independent processes that operate on work items at a specific board column/status. Each agent implements a common interface and runs as a plugin, using AI prompts or custom software. The factory composes automated agents alongside human workers (analysts, engineers, product managers).

3. **Clear Measure Intelligence Dashboard** — Visualizes week-over-week quality, stability, and speed metrics derived from work item movement events. Incorporates DORA research metrics, the five pillars from *Leadership for Effective Custom Software* by Jeffrey Palermo, and the Clear Measure scorecard template.

## Implementation Strategy

Sequential module delivery:

- **Phase 1 (Module 1):** Work Item Instrumentation — SQL Server database with DbUp migration scripts, webhook ingestion endpoints, event translation, status tracking, and reporting.
- **Phase 2 (Module 2):** Worker Agents — Plugin interface, agent runtime, agent registration, and integration with Module 1 events.
- **Phase 3 (Module 3):** Intelligence Dashboard — Metric aggregation from Module 1 events, week-over-week visualization, DORA metrics, and scorecard rendering.

## Scope

### In Scope

- Domain model for work items, statuses, status transitions, and work item types
- SQL Server schema via DbUp migration scripts (following existing `src/Database/scripts/Update/` pattern)
- Webhook receiver endpoints for popular work tracking systems (GitHub, Azure DevOps, Jira)
- Event translation layer converting webhooks into internal factory events
- Status timeline tracking with entry/exit timestamps per status per work item
- Throughput reports (total and per-status)
- Implicit defect detection (backward status transitions)
- Stuck work item identification (configurable staleness thresholds)
- Reports segmented by work item type
- Worker agent interface definition (`IWorkerAgent`)
- Agent plugin registration and execution model
- Intelligence dashboard metric aggregation and visualization

### Out of Scope

- Specific AI prompt implementations for worker agents (plug-in specific)
- Third-party work tracking system administration
- User authentication and authorization (leverages existing system)
- Mobile or native client interfaces

## Success Criteria

- Work items are tracked through all lifecycle statuses with accurate timestamps
- Webhook events from at least one external system (GitHub) are translated into factory events
- Throughput, implicit defect, and stuck item reports produce correct results
- Worker agent interface supports both AI-driven and traditional implementations
- Dashboard displays week-over-week DORA-aligned metrics
