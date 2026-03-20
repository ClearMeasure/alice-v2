CREATE TABLE [dbo].[WorkItemEvent]
(
	[Id] UNIQUEIDENTIFIER NOT NULL,
	[WorkItemExternalId] NVARCHAR(200) NOT NULL,
	[Source] NVARCHAR(50) NOT NULL,
	[EventType] NVARCHAR(100) NOT NULL,
	[PreviousStatus] NVARCHAR(200) NULL,
	[NewStatus] NVARCHAR(200) NOT NULL,
	[OccurredAtUtc] DATETIMEOFFSET NOT NULL,
	[ReceivedAtUtc] DATETIMEOFFSET NOT NULL,
	[RawPayload] NVARCHAR(MAX) NOT NULL,
	CONSTRAINT [PK_WorkItemEvent] PRIMARY KEY ([Id])
)
GO

CREATE INDEX [IX_WorkItemEvent_ExternalId_Source]
	ON [dbo].[WorkItemEvent] ([WorkItemExternalId], [Source])
GO

CREATE TABLE [dbo].[WorkItemState]
(
	[Id] UNIQUEIDENTIFIER NOT NULL,
	[ExternalId] NVARCHAR(200) NOT NULL,
	[Source] NVARCHAR(50) NOT NULL,
	[Title] NVARCHAR(500) NOT NULL,
	[CurrentStatus] NVARCHAR(200) NOT NULL,
	[ProjectName] NVARCHAR(200) NOT NULL,
	[LastUpdatedAtUtc] DATETIMEOFFSET NOT NULL,
	CONSTRAINT [PK_WorkItemState] PRIMARY KEY ([Id])
)
GO

CREATE UNIQUE INDEX [IX_WorkItemState_ExternalId_Source]
	ON [dbo].[WorkItemState] ([ExternalId], [Source])
GO
