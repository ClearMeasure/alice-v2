CREATE TABLE [dbo].[FactoryEvent]
(
	[Id]					UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[EventTypeCode]			NVARCHAR(50) NOT NULL,
	[FactoryWorkItemId]		UNIQUEIDENTIFIER NULL,
	[ExternalId]			NVARCHAR(200) NOT NULL,
	[ExternalSystem]		NVARCHAR(50) NOT NULL,
	[Payload]				NVARCHAR(MAX) NULL,
	[OccurredAt]			DATETIMEOFFSET NOT NULL,

	CONSTRAINT [FK_FactoryEvent_FactoryWorkItem] FOREIGN KEY ([FactoryWorkItemId])
		REFERENCES [dbo].[FactoryWorkItem] ([Id]),

	INDEX [IX_FactoryEvent_WorkItem] NONCLUSTERED ([FactoryWorkItemId]),
	INDEX [IX_FactoryEvent_OccurredAt] NONCLUSTERED ([OccurredAt])
)
