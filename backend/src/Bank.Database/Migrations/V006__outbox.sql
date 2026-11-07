-- Transactional outbox: events are written in the same transaction as the change they describe, and a
-- background publisher forwards them to the message broker afterwards.
--
-- Publishing straight to the broker from the request would leave two bad cases: the transfer commits but the
-- broker is down (event lost), or the event is sent and the transfer then rolls back (event about something
-- that never happened). With the outbox, an event exists exactly when its transfer does.
--
-- Delivery is at least once: if the publisher stops between sending and marking a row, the row is sent again.
-- Consumers deduplicate on MessageId.

CREATE SCHEMA messaging;
GO

CREATE TABLE messaging.OutboxMessages
(
    MessageId    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY NONCLUSTERED,
    -- Publishing follows this order, so consumers see events in the order they happened.
    Sequence     BIGINT IDENTITY(1,1) NOT NULL,
    Type         VARCHAR(100)   NOT NULL,
    Payload      NVARCHAR(MAX)  NOT NULL,
    OccurredAt   DATETIME2(3)   NOT NULL,
    PublishedAt  DATETIME2(3)   NULL,
    Attempts     INT            NOT NULL CONSTRAINT DF_OutboxMessages_Attempts DEFAULT 0,
    LastError    NVARCHAR(1000) NULL,

    CONSTRAINT CK_OutboxMessages_Payload CHECK (ISJSON(Payload) = 1)
);

CREATE UNIQUE CLUSTERED INDEX CX_OutboxMessages_Sequence ON messaging.OutboxMessages (Sequence);

-- The publisher only ever looks at what is still pending.
CREATE INDEX IX_OutboxMessages_Pending ON messaging.OutboxMessages (Sequence) WHERE PublishedAt IS NULL;
