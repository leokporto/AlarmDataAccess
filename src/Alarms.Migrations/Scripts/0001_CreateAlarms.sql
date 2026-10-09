CREATE TABLE dbo.Alarms
(
    Id                 int           IDENTITY(1,1) NOT NULL,
    Name               nvarchar(100) NOT NULL,
    State              tinyint       NOT NULL,
    Condition          tinyint       NOT NULL,
    Limit              float         NOT NULL,
    Priority           tinyint       NOT NULL,
    Timestamp_UTCTicks bigint        NOT NULL,
    Message            nvarchar(500) NOT NULL,
    CONSTRAINT PK_Alarms PRIMARY KEY (Id),
    CONSTRAINT CK_Alarms_Priority CHECK (Priority BETWEEN 1 AND 10)
);

CREATE UNIQUE INDEX UX_Alarms_Name ON dbo.Alarms (Name);
