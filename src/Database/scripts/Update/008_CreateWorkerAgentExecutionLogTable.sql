CREATE TABLE [dbo].[WorkerAgentExecutionLog]
(
	[Id]					UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[AgentName]				NVARCHAR(200) NOT NULL,
	[FactoryWorkItemId]		UNIQUEIDENTIFIER NOT NULL,
	[StartedAt]				DATETIMEOFFSET NOT NULL,
	[CompletedAt]			DATETIMEOFFSET NULL,
	[Success]				BIT NULL,
	[Summary]				NVARCHAR(MAX) NULL,
	[OutputData]			NVARCHAR(MAX) NULL,

	CONSTRAINT [FK_WorkerAgentExecutionLog_WorkItem] FOREIGN KEY ([FactoryWorkItemId])
		REFERENCES [dbo].[FactoryWorkItem] ([Id])
)
