CREATE TABLE [dbo].[DashboardMetricSnapshot]
(
	[Id]			UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[MetricName]	NVARCHAR(100) NOT NULL,
	[Category]		NVARCHAR(50) NOT NULL,
	[Value]			DECIMAL(18,4) NOT NULL,
	[PeriodStart]	DATETIMEOFFSET NOT NULL,
	[PeriodEnd]		DATETIMEOFFSET NOT NULL,
	[ComputedAt]	DATETIMEOFFSET NOT NULL,

	INDEX [IX_MetricSnapshot_Period] NONCLUSTERED ([PeriodStart], [PeriodEnd]),
	INDEX [IX_MetricSnapshot_Name] NONCLUSTERED ([MetricName])
)
