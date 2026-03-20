# Tasks: AI Software Factory System

Implementation is sequential by module. Module 1 is implemented first.

---

## Phase 1: Module 1 — Work Item Instrumentation

### 1.1 Database Schema

- [ ] **Create `004_CreateFactoryWorkItemTable.sql`** — DbUp migration script in `src/Database/scripts/Update/`. Creates the `FactoryWorkItem` table with columns: Id (PK), ExternalId, ExternalSystem, Title, WorkItemTypeCode, CurrentStatusCode, CreatedDate, LastStatusChangeDate. Add unique constraint on (ExternalId, ExternalSystem). Use TABS for indentation per project conventions.

- [ ] **Create `005_CreateStatusTransitionTable.sql`** — Creates the `StatusTransition` table with columns: Id (PK), FactoryWorkItemId (FK), FromStatusCode, ToStatusCode, TransitionDate, IsBackward. Add index on FactoryWorkItemId.

- [ ] **Create `006_CreateFactoryEventTable.sql`** — Creates the `FactoryEvent` table with columns: Id (PK), EventTypeCode, FactoryWorkItemId (FK, nullable), ExternalId, ExternalSystem, Payload (NVARCHAR(MAX)), OccurredAt. Add indexes on FactoryWorkItemId and OccurredAt.

### 1.2 Core Domain Model

- [ ] **Create `FactoryStatus` smart enum** — `src/Core/Model/Factory/FactoryStatus.cs`. Implement as a smart enum following the existing `WorkOrderStatus` pattern with `FromCode()` and `FromKey()` factory methods. Statuses: Conceptual (0), DesignInProgress (1), DesignComplete (2), DevelopmentInProgress (3), DevelopmentComplete (4), ReviewRequested (5), ReviewComplete (6), TestingInProgress (7), TestingComplete (8), DeploymentInProgress (9), Deployed (10), StabilizationInProgress (11), Stable (12), Cancelled (-1). Include ProgressionIndex property for backward detection.

- [ ] **Create `WorkItemType` smart enum** — `src/Core/Model/Factory/WorkItemType.cs`. Values: Feature, Bug, Chore, Spike, Hotfix. Include `FromCode()` and `FromKey()` factory methods.

- [ ] **Create `StatusTransition` value object** — `src/Core/Model/Factory/StatusTransition.cs`. Properties: FromStatus, ToStatus, TransitionDate, computed IsBackward property (compares progression indexes).

- [ ] **Create `FactoryWorkItem` entity** — `src/Core/Model/Factory/FactoryWorkItem.cs`. Extends `EntityBase<FactoryWorkItem>`. Properties per design. Methods: `ChangeStatus(newStatus, timestamp)` that creates a `StatusTransition` and updates `CurrentStatus`/`LastStatusChangeDate`; `IsStuck(threshold)` that checks if `LastStatusChangeDate` exceeds threshold; `HasImplicitDefect()` that checks if any transition in `StatusHistory` is backward.

- [ ] **Create `FactoryEventType` smart enum** — `src/Core/Model/Factory/FactoryEventType.cs`. Values: StatusChanged, BuildSucceeded, BuildFailed, PullRequestOpened, PullRequestMerged, DeploymentStarted, DeploymentCompleted, DeploymentFailed, TestsPassed, TestsFailed.

- [ ] **Create `FactoryEvent` notification** — `src/Core/Model/Factory/FactoryEvent.cs`. Implements `MediatR.INotification`. Properties: EventType, WorkItemId, ExternalId, ExternalSystem, Payload (Dictionary), OccurredAt.

### 1.3 Core Interfaces and Queries

- [ ] **Create `IWebhookTranslator` interface** — `src/Core/Interfaces/IWebhookTranslator.cs`. Method: `FactoryEvent? Translate(string system, string payload)`. The interface lives in Core with no external dependencies.

- [ ] **Create `ThroughputReportQuery`** — `src/Core/Queries/Factory/ThroughputReportQuery.cs`. MediatR request. Parameters: DateRange, optional StatusFilter, optional WorkItemTypeFilter. Returns list of throughput records.

- [ ] **Create `ImplicitDefectsQuery`** — `src/Core/Queries/Factory/ImplicitDefectsQuery.cs`. MediatR request. Parameters: DateRange, optional WorkItemTypeFilter. Returns work items with backward transitions.

- [ ] **Create `StuckWorkItemsQuery`** — `src/Core/Queries/Factory/StuckWorkItemsQuery.cs`. MediatR request. Parameters: StalenessThreshold (TimeSpan), optional StatusFilter, optional WorkItemTypeFilter. Returns work items exceeding threshold.

- [ ] **Create `ThroughputByStatusQuery`** — `src/Core/Queries/Factory/ThroughputByStatusQuery.cs`. MediatR request. Parameters: DateRange, optional WorkItemTypeFilter. Returns average time-in-status per status.

- [ ] **Create `WorkItemTimelineQuery`** — `src/Core/Queries/Factory/WorkItemTimelineQuery.cs`. MediatR request. Parameter: WorkItemId. Returns full status history for a single item.

### 1.4 DataAccess — EF Core Configuration

- [ ] **Add `FactoryWorkItem` to `DataContext`** — Add `DbSet<FactoryWorkItem>`, `DbSet<StatusTransition>`, and `DbSet<FactoryEvent>` properties to the existing `DataContext`. Configure entity mappings (smart enum value conversions for FactoryStatus, WorkItemType, FactoryEventType).

### 1.5 DataAccess — Event Handlers

- [ ] **Create `StatusChangeHandler`** — `src/DataAccess/Handlers/Factory/StatusChangeHandler.cs`. MediatR notification handler for `FactoryEvent` where EventType is StatusChanged. Looks up or creates `FactoryWorkItem`, calls `ChangeStatus()`, persists via `DataContext`.

- [ ] **Create `EventLogHandler`** — `src/DataAccess/Handlers/Factory/EventLogHandler.cs`. MediatR notification handler for `FactoryEvent`. Persists every factory event to the FactoryEvent table.

### 1.6 DataAccess — Query Handlers

- [ ] **Create `ThroughputReportHandler`** — `src/DataAccess/Handlers/Factory/ThroughputReportHandler.cs`. Queries StatusTransition table for items reaching terminal statuses within the date range.

- [ ] **Create `ImplicitDefectsHandler`** — `src/DataAccess/Handlers/Factory/ImplicitDefectsHandler.cs`. Queries StatusTransition table for backward transitions (IsBackward = true).

- [ ] **Create `StuckWorkItemsHandler`** — `src/DataAccess/Handlers/Factory/StuckWorkItemsHandler.cs`. Queries FactoryWorkItem table for items where LastStatusChangeDate is older than threshold.

- [ ] **Create `ThroughputByStatusHandler`** — `src/DataAccess/Handlers/Factory/ThroughputByStatusHandler.cs`. Computes average duration per status from StatusTransition timestamps.

- [ ] **Create `WorkItemTimelineHandler`** — `src/DataAccess/Handlers/Factory/WorkItemTimelineHandler.cs`. Returns ordered StatusTransition list for a given work item.

### 1.7 Webhook Translators

- [ ] **Create `GitHubWebhookTranslator`** — `src/DataAccess/Translators/GitHubWebhookTranslator.cs`. Implements `IWebhookTranslator`. Parses GitHub webhook JSON payloads for: push events, pull request events, check suite events, deployment events. Maps to appropriate `FactoryEvent`.

- [ ] **Create `AzureDevOpsWebhookTranslator`** — `src/DataAccess/Translators/AzureDevOpsWebhookTranslator.cs`. Implements `IWebhookTranslator`. Parses Azure DevOps service hook payloads for: work item updated, build completed, release deployment completed.

- [ ] **Create `JiraWebhookTranslator`** — `src/DataAccess/Translators/JiraWebhookTranslator.cs`. Implements `IWebhookTranslator`. Parses Jira webhook payloads for: issue updated, issue transitioned.

### 1.8 API Endpoint

- [ ] **Create `WebhookController`** — `src/UI/Api/Controllers/WebhookController.cs`. POST endpoint `api/webhooks/{system}` that accepts raw JSON body, resolves the appropriate `IWebhookTranslator`, translates the payload, and publishes the resulting `FactoryEvent` via `IBus`.

### 1.9 Unit Tests

- [ ] **Test `FactoryWorkItem.ChangeStatus`** — Verify status transitions update CurrentStatus, add to StatusHistory, update LastStatusChangeDate. Verify backward transitions set IsBackward on the transition. Use NUnit 4 + Shouldly assertions, AAA pattern, `Should` prefix naming.

- [ ] **Test `FactoryWorkItem.IsStuck`** — Verify returns true when LastStatusChangeDate exceeds threshold, false otherwise.

- [ ] **Test `FactoryWorkItem.HasImplicitDefect`** — Verify returns true when any backward transition exists.

- [ ] **Test `FactoryStatus` smart enum** — Verify `FromCode()`, `FromKey()`, progression index ordering.

- [ ] **Test `GitHubWebhookTranslator`** — Verify correct mapping of GitHub push, PR, and check suite webhook payloads to FactoryEvent.

### 1.10 Integration Tests

- [ ] **Test StatusChangeHandler end-to-end** — Publish a FactoryEvent, verify FactoryWorkItem is created/updated in DB with correct status and transition history.

- [ ] **Test query handlers** — Seed test data, execute ThroughputReportQuery/ImplicitDefectsQuery/StuckWorkItemsQuery, verify correct results.

---

## Phase 2: Module 2 — Worker Agents

### 2.1 Database Schema

- [ ] **Create `007_CreateWorkerAgentRegistrationTable.sql`** — WorkerAgentRegistration table: Id, AgentName, TargetStatusCode, AgentType, Configuration (JSON), IsActive, CreatedDate.

- [ ] **Create `008_CreateWorkerAgentExecutionLogTable.sql`** — WorkerAgentExecutionLog table: Id, AgentRegistrationId (FK), FactoryWorkItemId (FK), StartedAt, CompletedAt, Success, Summary, OutputData.

### 2.2 Core Interfaces

- [ ] **Create `IWorkerAgent` interface** — `src/Core/Model/Agents/IWorkerAgent.cs`. Properties: AgentName, TargetStatus. Method: `ExecuteAsync(FactoryWorkItem, CancellationToken)` returning `WorkerAgentResult`.

- [ ] **Create `WorkerAgentResult` record** — `src/Core/Model/Agents/WorkerAgentResult.cs`. Properties: Success, NextStatus, Summary, OutputData.

- [ ] **Create `IWorkerAgentRegistry` interface** — `src/Core/Model/Agents/IWorkerAgentRegistry.cs`. Methods: `RegisterAgent(IWorkerAgent)`, `GetAgents(FactoryStatus)`, `GetAllRegistrations()`.

### 2.3 DataAccess — Agent Runtime

- [ ] **Create `WorkerAgentRegistry`** — `src/DataAccess/Handlers/Factory/WorkerAgentRegistry.cs`. Implements `IWorkerAgentRegistry`. Manages in-process agent registrations and loads remote agent configurations from DB.

- [ ] **Create `AgentDispatchHandler`** — `src/DataAccess/Handlers/Factory/AgentDispatchHandler.cs`. MediatR notification handler for `FactoryEvent` (StatusChanged). Resolves agents from registry, executes them, logs results, and publishes follow-up status changes.

- [ ] **Create `RemoteWorkerAgent`** — `src/DataAccess/Handlers/Factory/RemoteWorkerAgent.cs`. Adapter implementing `IWorkerAgent` that proxies to an external HTTP endpoint.

### 2.4 Tests

- [ ] **Test WorkerAgentRegistry** — Verify agent registration and lookup by status.

- [ ] **Test AgentDispatchHandler** — Verify dispatching to registered agents on status change events, logging execution results.

---

## Phase 3: Module 3 — Intelligence Dashboard

### 3.1 Database Schema

- [ ] **Create `009_CreateDashboardMetricSnapshotTable.sql`** — DashboardMetricSnapshot table: Id, MetricName, Category, Value, PeriodStart, PeriodEnd, ComputedAt. Indexes on period and metric name.

### 3.2 Core Model and Queries

- [ ] **Create `DashboardMetric` model** — `src/Core/Model/Factory/DashboardMetric.cs`. Properties: MetricName, Category, Value, PeriodStart, PeriodEnd, Trend.

- [ ] **Create `MetricCategory` enum** — `src/Core/Model/Factory/MetricCategory.cs`. Values: Quality, Stability, Speed, Leadership.

- [ ] **Create `WeekOverWeekMetricsQuery`** — `src/Core/Queries/Factory/WeekOverWeekMetricsQuery.cs`. Returns current vs previous week metrics.

- [ ] **Create `ScoreCardQuery`** — `src/Core/Queries/Factory/ScoreCardQuery.cs`. Returns composite scorecard across all categories.

### 3.3 DataAccess — Metric Computation

- [ ] **Create `MetricComputeHandler`** — `src/DataAccess/Handlers/Factory/MetricComputeHandler.cs`. Listens for FactoryEvents, computes DORA metrics (Deployment Frequency, Lead Time, Change Failure Rate, MTTR) and Five Pillar metrics from StatusTransition data, persists DashboardMetricSnapshot records.

- [ ] **Create `WeekOverWeekMetricsHandler`** — `src/DataAccess/Handlers/Factory/WeekOverWeekMetricsHandler.cs`. Queries DashboardMetricSnapshot for current and previous week, computes trends.

- [ ] **Create `ScoreCardHandler`** — `src/DataAccess/Handlers/Factory/ScoreCardHandler.cs`. Aggregates metrics into weighted composite scores per category.

### 3.4 Dashboard UI

- [ ] **Create dashboard Blazor page** — `src/UI/Client/Pages/FactoryDashboard.razor`. Displays week-over-week metrics with trend indicators, DORA metrics section, Five Pillars section, and Clear Measure scorecard.

### 3.5 Tests

- [ ] **Test DORA metric computation** — Verify Deployment Frequency, Lead Time, Change Failure Rate, and MTTR calculations from sample StatusTransition data.

- [ ] **Test scorecard aggregation** — Verify composite score computation with configurable weights.

- [ ] **Test dashboard component** — bUnit test for FactoryDashboard.razor rendering with sample metric data.
