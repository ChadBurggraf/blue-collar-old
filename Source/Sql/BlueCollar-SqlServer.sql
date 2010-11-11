IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[BlueCollar]') AND type IN (N'U'))
	DROP TABLE [BlueCollar]
GO
CREATE TABLE [BlueCollar]
(
	[Id] int NOT NULL IDENTITY(1, 1),
	[Name] varchar(128) NOT NULL,
	[JobType] varchar(512) NOT NULL,
	[Data] xml NOT NULL,
	[Status] varchar(24) NOT NULL,
	[Exception] xml NULL,
	[QueueDate] datetime NOT NULL,
	[StartDate] datetime NULL,
	[FinishDate] datetime NULL,
	[ScheduleName] varchar(128) NULL,
	[TryNumber] int NOT NULL,
	CONSTRAINT [PK_BlueCollar] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_BlueCollar_QueueDate_Status] ON [BlueCollar] 
(
	[QueueDate] ASC,
	[Status] ASC
) INCLUDE 
( 
	[Id],
	[Name],
	[JobType],
	[Data],
	[Exception],
	[StartDate],
	[FinishDate],
	[ScheduleName],
	[TryNumber]
) WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
CREATE STATISTICS [BlueCollar_Status_QueueDate] ON [BlueCollar]([Status], [QueueDate])
GO