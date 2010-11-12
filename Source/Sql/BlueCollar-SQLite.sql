CREATE TABLE [BlueCollar] 
(
	[Id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
	[Name] VARCHAR(128)  NOT NULL,
	[JobType] VARCHAR(512)  NOT NULL,
	[Data] TEXT  NOT NULL,
	[Status] VARCHAR(24)  NOT NULL,
	[Exception] TEST  NULL,
	[QueueDate] TIMESTAMP  NOT NULL,
	[StartDate] TIMESTAMP  NULL,
	[FinishDate] TIMESTAMP  NULL,
	[ScheduleName] VARCHAR(128)  NULL,
	[TryNumber] INTEGER  NOT NULL
);
CREATE INDEX [IX_BlueCollar_QueueDate_Status] ON [BlueCollar]([QueueDate], [Status]);