CREATE TABLE [dbo].[StatusTransition]
(
	[Id]					UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[FactoryWorkItemId]		UNIQUEIDENTIFIER NOT NULL,
	[FromStatusCode]		NVARCHAR(50) NULL,
	[ToStatusCode]			NVARCHAR(50) NOT NULL,
	[TransitionDate]		DATETIMEOFFSET NOT NULL,
	[IsBackward]			BIT NOT NULL,

	CONSTRAINT [FK_StatusTransition_FactoryWorkItem] FOREIGN KEY ([FactoryWorkItemId])
		REFERENCES [dbo].[FactoryWorkItem] ([Id]),

	INDEX [IX_StatusTransition_WorkItem] NONCLUSTERED ([FactoryWorkItemId])
)
