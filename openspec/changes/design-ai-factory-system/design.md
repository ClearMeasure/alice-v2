# Design: AI Software Factory System

## Architecture Overview

The AI Software Factory extends the existing onion architecture application. Three new bounded contexts (modules) are introduced, each following the established dependency rules: Core defines domain models and interfaces, DataAccess implements persistence and handlers, and the UI/API layer exposes endpoints.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        UI / API Layer                               │
│  ┌──────────────┐  ┌──────────────────┐  ┌───────────────────────┐ │
│  │  Webhook API  │  │  Dashboard UI    │  │  Agent Management API │ │
│  └──────┬───────┘  └────────┬─────────┘  └───────────┬───────────┘ │
│         │                   │                         │             │
│─────────┼───────────────────┼─────────────────────────┼─────────────│
│         ▼                   ▼                         ▼             │
│                      DataAccess Layer                               │
│  ┌──────────────┐  ┌──────────────────┐  ┌───────────────────────┐ │
│  │  Event        │  │  Metric          │  │  Agent Registry       │ │
│  │  Handlers     │  │  Handlers        │  │  Handlers             │ │
│  └──────┬───────┘  └────────┬─────────┘  └───────────┬───────────┘ │
│         │                   │                         │             │
│─────────┼───────────────────┼─────────────────────────┼─────────────│
│         ▼                   ▼                         ▼             │
│                        Core Layer                                   │
│  ┌──────────────┐  ┌──────────────────┐  ┌───────────────────────┐ │
│  │  Work Item    │  │  Metrics &       │  │  Worker Agent         │ │
│  │  Domain Model │  │  Reports Model   │  │  Interfaces           │ │
│  └──────────────┘  └──────────────────┘  └───────────────────────┘ │
│                                                                     │
│─────────────────────────────────────────────────────────────────────│
│                      Database (DbUp)                                │
│  SQL Server — Migration scripts in src/Database/scripts/Update/     │
└─────────────────────────────────────────────────────────────────────┘
```

## Module 1: Work Item Instrumentation

### Domain Model

#### FactoryWorkItem

The central aggregate root tracking a work item through the software delivery pipeline.

```
FactoryWorkItem : EntityBase<FactoryWorkItem>
├── ExternalId : string              — ID from external system (e.g., GitHub issue #)
├── ExternalSystem : string          — Source system identifier ("github", "azdo", "jira")
├── Title : string                   — Work item title
├── WorkItemType : WorkItemType      — Type classification (Feature, Bug, Chore, etc.)
├── CurrentStatus : FactoryStatus    — Current pipeline status
├── CreatedDate : DateTimeOffset     — When the item was first tracked
├── LastStatusChangeDate : DateTimeOffset — When the status last changed
├── StatusHistory : List<StatusTransition> — Full timeline of transitions
├── ChangeStatus(newStatus, timestamp) — Transition to a new status
├── IsStuck(threshold) : bool        — Whether the item exceeds staleness threshold
├── HasImplicitDefect() : bool       — Whether any backward transition exists
```

#### FactoryStatus (Smart Enum)

Pipeline statuses representing columns on the work tracking board. Ordered by progression index for detecting backward transitions.

| Status | Progression Index |
|--------|-------------------|
| Conceptual | 0 |
| DesignInProgress | 1 |
| DesignComplete | 2 |
| DevelopmentInProgress | 3 |
| DevelopmentComplete | 4 |
| ReviewRequested | 5 |
| ReviewComplete | 6 |
| TestingInProgress | 7 |
| TestingComplete | 8 |
| DeploymentInProgress | 9 |
| Deployed | 10 |
| StabilizationInProgress | 11 |
| Stable | 12 |
| Cancelled | -1 |

Implemented as a smart enum following the existing `WorkOrderStatus` pattern with `FromCode()` and `FromKey()` factory methods.

#### StatusTransition

Value object recording a single status change.

```
StatusTransition
├── FromStatus : FactoryStatus?      — Previous status (null for initial)
├── ToStatus : FactoryStatus         — New status
├── TransitionDate : DateTimeOffset   — When the transition occurred
├── IsBackward : bool                — Computed: ToStatus.Index < FromStatus.Index
```

#### WorkItemType (Smart Enum)

```
WorkItemType
├── Feature
├── Bug
├── Chore
├── Spike
├── Hotfix
```

#### FactoryEvent

Events produced by webhook translation, dispatched via MediatR.

```
FactoryEvent : INotification
├── EventType : FactoryEventType     — BuildSucceeded, BuildFailed, StatusChanged, etc.
├── WorkItemId : Guid?               — Associated work item (if applicable)
├── ExternalId : string              — External reference
├── ExternalSystem : string          — Source system
├── Payload : Dictionary<string, string> — Event-specific key-value data
├── OccurredAt : DateTimeOffset      — When the event occurred
```

#### FactoryEventType (Smart Enum)

```
FactoryEventType
├── StatusChanged
├── BuildSucceeded
├── BuildFailed
├── PullRequestOpened
├── PullRequestMerged
├── DeploymentStarted
├── DeploymentCompleted
├── DeploymentFailed
├── TestsPassed
├── TestsFailed
```

### Webhook Integration

```
External System ──HTTP POST──▶ WebhookController
                                    │
                                    ▼
                              IWebhookTranslator.Translate(system, payload)
                                    │
                                    ▼
                              FactoryEvent (MediatR notification)
                                    │
                          ┌─────────┴──────────┐
                          ▼                    ▼
                  StatusChangeHandler    EventLogHandler
                  (updates work item)    (persists event)
```

**IWebhookTranslator** — Interface in Core. Implementations in DataAccess per system:
- `GitHubWebhookTranslator`
- `AzureDevOpsWebhookTranslator`
- `JiraWebhookTranslator`

Each translator maps the external system's webhook payload into a `FactoryEvent`.

### Reporting

Reports are implemented as MediatR queries in Core, with handlers in DataAccess.

| Query | Description |
|-------|-------------|
| `ThroughputReportQuery` | Total items completed per time period, optionally filtered by status or type |
| `ThroughputByStatusQuery` | Time spent in each status, averaged across items |
| `ImplicitDefectsQuery` | Work items with backward status transitions |
| `StuckWorkItemsQuery` | Items exceeding staleness threshold per status, configurable by type |
| `WorkItemTimelineQuery` | Full status history for a single work item |

### Database Schema (Module 1)

New tables added via DbUp scripts following the existing `###_Description.sql` naming convention.

```sql
-- 004_CreateFactoryWorkItemTable.sql
FactoryWorkItem
├── Id : UNIQUEIDENTIFIER (PK)
├── ExternalId : NVARCHAR(200) NOT NULL
├── ExternalSystem : NVARCHAR(50) NOT NULL
├── Title : NVARCHAR(500) NOT NULL
├── WorkItemTypeCode : NVARCHAR(20) NOT NULL
├── CurrentStatusCode : NVARCHAR(50) NOT NULL
├── CreatedDate : DATETIMEOFFSET NOT NULL
├── LastStatusChangeDate : DATETIMEOFFSET NOT NULL
├── UNIQUE(ExternalId, ExternalSystem)

-- 005_CreateStatusTransitionTable.sql
StatusTransition
├── Id : UNIQUEIDENTIFIER (PK)
├── FactoryWorkItemId : UNIQUEIDENTIFIER (FK → FactoryWorkItem)
├── FromStatusCode : NVARCHAR(50) NULL
├── ToStatusCode : NVARCHAR(50) NOT NULL
├── TransitionDate : DATETIMEOFFSET NOT NULL
├── IsBackward : BIT NOT NULL
├── INDEX IX_StatusTransition_WorkItem (FactoryWorkItemId)

-- 006_CreateFactoryEventTable.sql
FactoryEvent
├── Id : UNIQUEIDENTIFIER (PK)
├── EventTypeCode : NVARCHAR(50) NOT NULL
├── FactoryWorkItemId : UNIQUEIDENTIFIER NULL (FK → FactoryWorkItem)
├── ExternalId : NVARCHAR(200) NOT NULL
├── ExternalSystem : NVARCHAR(50) NOT NULL
├── Payload : NVARCHAR(MAX) NULL
├── OccurredAt : DATETIMEOFFSET NOT NULL
├── INDEX IX_FactoryEvent_WorkItem (FactoryWorkItemId)
├── INDEX IX_FactoryEvent_OccurredAt (OccurredAt)
```

## Module 2: Worker Agents

### Interface Definition

```csharp
// In Core — no external dependencies
public interface IWorkerAgent
{
    string AgentName { get; }
    FactoryStatus TargetStatus { get; }
    Task<WorkerAgentResult> ExecuteAsync(FactoryWorkItem workItem, CancellationToken ct);
}

public record WorkerAgentResult(
    bool Success,
    FactoryStatus? NextStatus,
    string Summary,
    Dictionary<string, string> OutputData);
```

### Agent Runtime

```
FactoryEvent (StatusChanged) ──▶ AgentDispatcher
                                      │
                                      ▼
                               IWorkerAgentRegistry.GetAgent(status)
                                      │
                              ┌───────┴───────┐
                              ▼               ▼
                         AI Agent      Custom Agent
                      (prompt-based)   (code-based)
                              │               │
                              └───────┬───────┘
                                      ▼
                              WorkerAgentResult
                                      │
                                      ▼
                              IBus.Send(StatusChangeCommand)
```

**IWorkerAgentRegistry** — Manages agent registrations. Agents register per target status. Multiple agents can register for the same status (executed in sequence).

**AgentDispatcher** — MediatR notification handler that listens for `StatusChanged` events and invokes the registered agent for the new status.

### Plugin Model

Worker agents run as independent processes communicating via:
1. **In-process plugins** — Implement `IWorkerAgent`, loaded via assembly scanning (like existing MediatR handler registration)
2. **Out-of-process agents** — Communicate via HTTP or message queue. A `RemoteWorkerAgent` adapter implements `IWorkerAgent` and proxies to the external process.

### Database Schema (Module 2)

```sql
-- 007_CreateWorkerAgentRegistrationTable.sql
WorkerAgentRegistration
├── Id : UNIQUEIDENTIFIER (PK)
├── AgentName : NVARCHAR(200) NOT NULL
├── TargetStatusCode : NVARCHAR(50) NOT NULL
├── AgentType : NVARCHAR(20) NOT NULL  — 'InProcess' or 'Remote'
├── Configuration : NVARCHAR(MAX) NULL — JSON config (endpoint URL, prompt, etc.)
├── IsActive : BIT NOT NULL DEFAULT 1
├── CreatedDate : DATETIMEOFFSET NOT NULL

-- 008_CreateWorkerAgentExecutionLogTable.sql
WorkerAgentExecutionLog
├── Id : UNIQUEIDENTIFIER (PK)
├── AgentRegistrationId : UNIQUEIDENTIFIER (FK)
├── FactoryWorkItemId : UNIQUEIDENTIFIER (FK)
├── StartedAt : DATETIMEOFFSET NOT NULL
├── CompletedAt : DATETIMEOFFSET NULL
├── Success : BIT NULL
├── Summary : NVARCHAR(MAX) NULL
├── OutputData : NVARCHAR(MAX) NULL
```

## Module 3: Clear Measure Intelligence Dashboard

### Metric Model

```
DashboardMetric
├── MetricName : string
├── Category : MetricCategory       — Quality, Stability, Speed, Leadership
├── Value : decimal
├── PeriodStart : DateTimeOffset
├── PeriodEnd : DateTimeOffset
├── Trend : TrendDirection          — Up, Down, Stable
```

### Metric Categories

**DORA Metrics:**
| Metric | Derivation |
|--------|-----------|
| Deployment Frequency | Count of `Deployed` transitions per period |
| Lead Time for Changes | Avg time from `Conceptual` to `Deployed` |
| Change Failure Rate | Ratio of items with backward transitions post-deployment |
| Mean Time to Recovery | Avg time from failure event to next `Stable` transition |

**Five Pillars (Leadership for Effective Custom Software):**
| Pillar | Metric |
|--------|--------|
| Technical Excellence | Ratio of items passing review without rework |
| Continuous Improvement | Week-over-week throughput trend |
| Sustainable Pace | Avg time-in-status consistency (low variance = sustainable) |
| Business Alignment | Feature vs Bug vs Chore distribution |
| Team Empowerment | Agent automation ratio (automated vs manual transitions) |

**Clear Measure Scorecard:**
- Composite score derived from weighted DORA + Five Pillar metrics
- Configurable weights per organization

### Dashboard Queries

| Query | Description |
|-------|-------------|
| `WeekOverWeekMetricsQuery` | All metrics for current vs previous week |
| `MetricTrendQuery` | Time series for a specific metric over N weeks |
| `ScoreCardQuery` | Composite scorecard with all categories |
| `TeamVelocityQuery` | Throughput segmented by work item type over time |

### Database Schema (Module 3)

```sql
-- 009_CreateDashboardMetricSnapshotTable.sql
DashboardMetricSnapshot
├── Id : UNIQUEIDENTIFIER (PK)
├── MetricName : NVARCHAR(100) NOT NULL
├── Category : NVARCHAR(50) NOT NULL
├── Value : DECIMAL(18,4) NOT NULL
├── PeriodStart : DATETIMEOFFSET NOT NULL
├── PeriodEnd : DATETIMEOFFSET NOT NULL
├── ComputedAt : DATETIMEOFFSET NOT NULL
├── INDEX IX_MetricSnapshot_Period (PeriodStart, PeriodEnd)
├── INDEX IX_MetricSnapshot_Name (MetricName)
```

## CQRS Flow Integration

All operations follow the existing `IBus` → MediatR pattern:

```
Webhook POST → WebhookController → IBus.Send(TranslateWebhookCommand)
    → TranslateWebhookHandler → IWebhookTranslator → IBus.Publish(FactoryEvent)
        → StatusChangeHandler → FactoryWorkItem.ChangeStatus() → DataContext.SaveChangesAsync()
        → AgentDispatchHandler → IWorkerAgentRegistry → IWorkerAgent.ExecuteAsync()
        → MetricComputeHandler → DashboardMetricSnapshot → DataContext.SaveChangesAsync()
```

## File Organization

```
src/Core/
├── Model/
│   ├── Factory/
│   │   ├── FactoryWorkItem.cs
│   │   ├── FactoryStatus.cs
│   │   ├── StatusTransition.cs
│   │   ├── WorkItemType.cs
│   │   ├── FactoryEvent.cs
│   │   ├── FactoryEventType.cs
│   │   ├── DashboardMetric.cs
│   │   └── MetricCategory.cs
│   └── Agents/
│       ├── IWorkerAgent.cs
│       ├── IWorkerAgentRegistry.cs
│       └── WorkerAgentResult.cs
├── Queries/
│   └── Factory/
│       ├── ThroughputReportQuery.cs
│       ├── ImplicitDefectsQuery.cs
│       ├── StuckWorkItemsQuery.cs
│       ├── WeekOverWeekMetricsQuery.cs
│       └── ScoreCardQuery.cs
└── Interfaces/
    └── IWebhookTranslator.cs

src/DataAccess/
├── Handlers/Factory/
│   ├── StatusChangeHandler.cs
│   ├── AgentDispatchHandler.cs
│   ├── EventLogHandler.cs
│   ├── ThroughputReportHandler.cs
│   ├── ImplicitDefectsHandler.cs
│   ├── StuckWorkItemsHandler.cs
│   └── MetricComputeHandler.cs
└── Translators/
    ├── GitHubWebhookTranslator.cs
    ├── AzureDevOpsWebhookTranslator.cs
    └── JiraWebhookTranslator.cs

src/UI/Api/
└── Controllers/
    └── WebhookController.cs

src/Database/scripts/Update/
├── 004_CreateFactoryWorkItemTable.sql
├── 005_CreateStatusTransitionTable.sql
├── 006_CreateFactoryEventTable.sql
├── 007_CreateWorkerAgentRegistrationTable.sql
├── 008_CreateWorkerAgentExecutionLogTable.sql
└── 009_CreateDashboardMetricSnapshotTable.sql
```
