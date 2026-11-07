-- Transaction history the user brings in from other banks (CSV statements) or generates as demo data.
-- It feeds the analytics (category trends, recurring payment and anomaly detection); the banking core's
-- dbo.Transactions stays tied to this bank's own accounts.

-- One row per upload: who imported what and with which result. Keeps an audit trail and
-- makes it possible to identify (and later undo) everything that came from a single file.
CREATE TABLE finance.ImportBatches
(
    ImportBatchId        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ImportBatches PRIMARY KEY,
    UserId               BIGINT         NOT NULL CONSTRAINT FK_ImportBatches_Users REFERENCES dbo.Users (UserId),
    Source               VARCHAR(10)    NOT NULL,
    FileName             NVARCHAR(260)  NULL,
    TotalRows            INT            NOT NULL,
    ImportedRows         INT            NOT NULL,
    DuplicateRows        INT            NOT NULL,
    CreatedAt            DATETIME2(3)   NOT NULL CONSTRAINT DF_ImportBatches_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_ImportBatches_Source CHECK (Source IN ('Csv', 'Synthetic')),
    CONSTRAINT CK_ImportBatches_Counts CHECK (ImportedRows + DuplicateRows = TotalRows)
);

CREATE INDEX IX_ImportBatches_UserId ON finance.ImportBatches (UserId);

-- Amount is positive and Direction carries the sign, as in dbo.Transactions.
-- Fingerprint identifies a statement line so that importing the same or an overlapping statement
-- twice does not duplicate it; see the import service for how it is computed.
CREATE TABLE finance.Transactions
(
    TransactionId   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FinanceTransactions PRIMARY KEY,
    UserId          BIGINT           NOT NULL CONSTRAINT FK_FinanceTransactions_Users REFERENCES dbo.Users (UserId),
    ImportBatchId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_FinanceTransactions_ImportBatches REFERENCES finance.ImportBatches (ImportBatchId),
    BookingDate     DATE             NOT NULL,
    Description     NVARCHAR(200)    NOT NULL,
    Amount          DECIMAL(19,4)    NOT NULL,
    Direction       VARCHAR(10)      NOT NULL,
    CurrencyCode    CHAR(3)          NOT NULL,
    Category        NVARCHAR(50)     NULL,
    Fingerprint     BINARY(32)       NOT NULL,
    CreatedAt       DATETIME2(3)     NOT NULL CONSTRAINT DF_FinanceTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
    DeletedAt       DATETIME2(3)     NULL,

    CONSTRAINT CK_FinanceTransactions_Amount CHECK (Amount > 0),
    CONSTRAINT CK_FinanceTransactions_Direction CHECK (Direction IN ('Inbound', 'Outbound')),
    CONSTRAINT CK_FinanceTransactions_Currency CHECK (CurrencyCode IN ('TRY', 'USD', 'EUR'))
);

-- The database, not the application, is what finally guarantees a line is stored only once.
-- Filtered on live rows so a deleted import can be uploaded again.
CREATE UNIQUE INDEX UX_FinanceTransactions_User_Fingerprint
    ON finance.Transactions (UserId, Fingerprint)
    WHERE DeletedAt IS NULL;

-- Analytics read a user's history by date range.
CREATE INDEX IX_FinanceTransactions_User_Date
    ON finance.Transactions (UserId, BookingDate)
    INCLUDE (Amount, Direction, Category)
    WHERE DeletedAt IS NULL;
