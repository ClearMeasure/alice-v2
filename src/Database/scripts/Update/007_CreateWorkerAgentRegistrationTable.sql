CREATE TABLE [dbo].[WorkerAgentRegistration]
(
	[Id]				UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[AgentName]			NVARCHAR(200) NOT NULL,
	[TargetStatusCode]	NVARCHAR(50) NOT NULL,
	[AgentType]			NVARCHAR(20) NOT NULL,
	[Configuration]		NVARCHAR(MAX) NULL,
	[IsActive]			BIT NOT NULL DEFAULT 1,
	[CreatedDate]		DATETIMEOFFSET NOT NULL
)
