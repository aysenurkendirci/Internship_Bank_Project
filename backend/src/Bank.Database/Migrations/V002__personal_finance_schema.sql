-- Personal finance module: user-entered credit cards, recurring cash flows and installment plans.
-- Kept in its own schema so it stays separate from the banking core tables in dbo.
--
-- Every table carries:
--   * UserId                       -> every query is scoped to the owner
--   * CreatedAt / UpdatedAt        -> audit trail
--   * DeletedAt                    -> soft delete; financial records are never physically removed
--   * RowVersion                   -> optimistic concurrency; a stale update is rejected, not silently applied
--
-- CHECK constraints repeat the domain invariants on purpose: the database is the last line of
-- defence against rows written by scripts, imports or a future bug in the application.

CREATE SCHEMA finance;
GO

CREATE TABLE finance.CreditCards
(
    CreditCardId    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CreditCards PRIMARY KEY,
    UserId          BIGINT         NOT NULL CONSTRAINT FK_CreditCards_Users REFERENCES dbo.Users (UserId),
    Name            NVARCHAR(100)  NOT NULL,
    LimitAmount     DECIMAL(19,4)  NOT NULL,
    CurrencyCode    CHAR(3)        NOT NULL,
    StatementDay    TINYINT        NOT NULL,
    PaymentDueDays  TINYINT        NOT NULL,
    CreatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_CreditCards_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2(3)   NULL,
    DeletedAt       DATETIME2(3)   NULL,
    RowVersion      ROWVERSION     NOT NULL,

    -- Target of the composite foreign keys below: lets the database guarantee that a card
    -- referenced by another row belongs to the same user.
    CONSTRAINT UQ_CreditCards_Id_User UNIQUE (CreditCardId, UserId),
    CONSTRAINT CK_CreditCards_Limit CHECK (LimitAmount > 0),
    CONSTRAINT CK_CreditCards_Currency CHECK (CurrencyCode IN ('TRY', 'USD', 'EUR')),
    CONSTRAINT CK_CreditCards_StatementDay CHECK (StatementDay BETWEEN 1 AND 31),
    CONSTRAINT CK_CreditCards_PaymentDueDays CHECK (PaymentDueDays BETWEEN 1 AND 30)
);

CREATE INDEX IX_CreditCards_UserId ON finance.CreditCards (UserId) WHERE DeletedAt IS NULL;

CREATE TABLE finance.RecurringCashFlows
(
    RecurringCashFlowId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecurringCashFlows PRIMARY KEY,
    UserId              BIGINT         NOT NULL CONSTRAINT FK_RecurringCashFlows_Users REFERENCES dbo.Users (UserId),
    CreditCardId        BIGINT         NULL,
    Name                NVARCHAR(100)  NOT NULL,
    Kind                VARCHAR(20)    NOT NULL,
    Category            NVARCHAR(50)   NOT NULL,
    Amount              DECIMAL(19,4)  NOT NULL,
    CurrencyCode        CHAR(3)        NOT NULL,
    Frequency           VARCHAR(10)    NOT NULL,
    RecurrenceInterval  SMALLINT       NOT NULL,
    StartDate           DATE           NOT NULL,
    EndDate             DATE           NULL,
    CreatedAt           DATETIME2(3)   NOT NULL CONSTRAINT DF_RecurringCashFlows_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2(3)   NULL,
    DeletedAt           DATETIME2(3)   NULL,
    RowVersion          ROWVERSION     NOT NULL,

    CONSTRAINT FK_RecurringCashFlows_CreditCards FOREIGN KEY (CreditCardId, UserId)
        REFERENCES finance.CreditCards (CreditCardId, UserId),
    CONSTRAINT CK_RecurringCashFlows_Kind CHECK (Kind IN ('Income', 'Expense', 'Subscription')),
    CONSTRAINT CK_RecurringCashFlows_Amount CHECK (Amount > 0),
    CONSTRAINT CK_RecurringCashFlows_Currency CHECK (CurrencyCode IN ('TRY', 'USD', 'EUR')),
    CONSTRAINT CK_RecurringCashFlows_Frequency CHECK (Frequency IN ('Weekly', 'Monthly', 'Yearly')),
    CONSTRAINT CK_RecurringCashFlows_Interval CHECK (RecurrenceInterval >= 1),
    CONSTRAINT CK_RecurringCashFlows_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT CK_RecurringCashFlows_IncomeNotOnCard CHECK (Kind <> 'Income' OR CreditCardId IS NULL)
);

CREATE INDEX IX_RecurringCashFlows_UserId ON finance.RecurringCashFlows (UserId) WHERE DeletedAt IS NULL;

CREATE TABLE finance.InstallmentPlans
(
    InstallmentPlanId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InstallmentPlans PRIMARY KEY,
    UserId            BIGINT         NOT NULL CONSTRAINT FK_InstallmentPlans_Users REFERENCES dbo.Users (UserId),
    CreditCardId      BIGINT         NOT NULL,
    Description       NVARCHAR(200)  NOT NULL,
    TotalAmount       DECIMAL(19,4)  NOT NULL,
    CurrencyCode      CHAR(3)        NOT NULL,
    InstallmentCount  TINYINT        NOT NULL,
    PurchaseDate      DATE           NOT NULL,
    CreatedAt         DATETIME2(3)   NOT NULL CONSTRAINT DF_InstallmentPlans_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt         DATETIME2(3)   NULL,
    DeletedAt         DATETIME2(3)   NULL,
    RowVersion        ROWVERSION     NOT NULL,

    CONSTRAINT FK_InstallmentPlans_CreditCards FOREIGN KEY (CreditCardId, UserId)
        REFERENCES finance.CreditCards (CreditCardId, UserId),
    CONSTRAINT CK_InstallmentPlans_TotalAmount CHECK (TotalAmount > 0),
    CONSTRAINT CK_InstallmentPlans_Currency CHECK (CurrencyCode IN ('TRY', 'USD', 'EUR')),
    CONSTRAINT CK_InstallmentPlans_Count CHECK (InstallmentCount BETWEEN 1 AND 36)
);

CREATE INDEX IX_InstallmentPlans_UserId ON finance.InstallmentPlans (UserId) WHERE DeletedAt IS NULL;
CREATE INDEX IX_InstallmentPlans_CreditCardId ON finance.InstallmentPlans (CreditCardId) WHERE DeletedAt IS NULL;
