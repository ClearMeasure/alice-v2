CREATE TABLE [dbo].[FactoryWorkItem]
(
	[Id]					UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[ExternalId]			NVARCHAR(200) NOT NULL,
	[ExternalSystem]		NVARCHAR(50) NOT NULL,
	[Title]					NVARCHAR(500) NOT NULL,
	[WorkItemTypeCode]		NVARCHAR(20) NOT NULL,
	[CurrentStatusCode]		NVARCHAR(50) NOT NULL,
	[CreatedDate]			DATETIMEOFFSET NOT NULL,
	[LastStatusChangeDate]	DATETIMEOFFSET NOT NULL,

	CONSTRAINT [UQ_FactoryWorkItem_External] UNIQUE ([ExternalId], [ExternalSystem])
)
