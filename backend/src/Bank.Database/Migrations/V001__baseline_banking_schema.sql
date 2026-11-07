-- Baseline schema for the banking core: users, accounts, cards, transactions.
-- Recreates what the application previously read from the Oracle PKG_AUTH / PKG_DASHBOARD packages.
--
-- Conventions (see docs/adr/0002):
--   * money       -> DECIMAL(19,4), never FLOAT
--   * timestamps  -> DATETIME2 in UTC
--   * enum-like columns are guarded by CHECK constraints

CREATE TABLE dbo.Users
(
    UserId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    TcNo          CHAR(11)       NOT NULL,
    FirstName     NVARCHAR(100)  NOT NULL,
    LastName      NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(256)  NOT NULL,
    Phone         VARCHAR(20)    NOT NULL,
    Membership    VARCHAR(20)    NOT NULL,
    PasswordHash  VARCHAR(512)   NOT NULL,
    CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAt   DATETIME2(3)   NULL,

    CONSTRAINT UQ_Users_TcNo  UNIQUE (TcNo),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Membership CHECK (Membership IN ('Personal', 'Corporate'))
);

CREATE TABLE dbo.Accounts
(
    AccountId    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
    UserId       BIGINT         NOT NULL CONSTRAINT FK_Accounts_Users REFERENCES dbo.Users (UserId),
    AccountType  VARCHAR(20)    NOT NULL,
    CurrencyCode CHAR(3)        NOT NULL,
    Iban         CHAR(26)       NOT NULL,
    Balance      DECIMAL(19,4)  NOT NULL CONSTRAINT DF_Accounts_Balance DEFAULT 0,
    Status       VARCHAR(20)    NOT NULL CONSTRAINT DF_Accounts_Status DEFAULT 'Active',
    CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Accounts_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT UQ_Accounts_Iban UNIQUE (Iban),
    CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN ('Demand', 'Time', 'ForeignCurrency')),
    CONSTRAINT CK_Accounts_Status CHECK (Status IN ('Active', 'Frozen', 'Closed'))
);

CREATE INDEX IX_Accounts_UserId ON dbo.Accounts (UserId);

-- Only the last four digits of the card number are stored (PCI DSS data minimisation);
-- the full PAN would belong in a tokenisation vault, not in this database.
CREATE TABLE dbo.Cards
(
    CardId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cards PRIMARY KEY,
    UserId        BIGINT         NOT NULL CONSTRAINT FK_Cards_Users REFERENCES dbo.Users (UserId),
    AccountId     BIGINT         NOT NULL CONSTRAINT FK_Cards_Accounts REFERENCES dbo.Accounts (AccountId),
    CardLastFour  CHAR(4)        NOT NULL,
    CardType      VARCHAR(20)    NOT NULL,
    IsVirtual     BIT            NOT NULL CONSTRAINT DF_Cards_IsVirtual DEFAULT 0,
    Status        VARCHAR(20)    NOT NULL CONSTRAINT DF_Cards_Status DEFAULT 'Active',
    Contactless   BIT            NOT NULL CONSTRAINT DF_Cards_Contactless DEFAULT 1,
    OnlineUse     BIT            NOT NULL CONSTRAINT DF_Cards_OnlineUse DEFAULT 1,
    DailyLimit    DECIMAL(19,4)  NOT NULL,
    MonthlyLimit  DECIMAL(19,4)  NOT NULL,
    CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Cards_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_Cards_CardType CHECK (CardType IN ('Debit', 'Credit')),
    CONSTRAINT CK_Cards_Status CHECK (Status IN ('Active', 'Blocked', 'Cancelled')),
    CONSTRAINT CK_Cards_Limits CHECK (DailyLimit >= 0 AND MonthlyLimit >= DailyLimit)
);

CREATE INDEX IX_Cards_UserId ON dbo.Cards (UserId);

-- Amount is always positive; Direction carries the sign. This keeps SUM queries explicit
-- and prevents a negative "Inbound" row from silently reversing meaning.
CREATE TABLE dbo.Transactions
(
    TransactionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
    AccountId     BIGINT         NOT NULL CONSTRAINT FK_Transactions_Accounts REFERENCES dbo.Accounts (AccountId),
    Amount        DECIMAL(19,4)  NOT NULL,
    Direction     VARCHAR(10)    NOT NULL,
    Category      NVARCHAR(50)   NOT NULL,
    Description   NVARCHAR(200)  NOT NULL,
    CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_Transactions_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Transactions_Direction CHECK (Direction IN ('Inbound', 'Outbound'))
);

-- Serves "latest N transactions of an account" without a sort step.
CREATE INDEX IX_Transactions_AccountId_CreatedAt ON dbo.Transactions (AccountId, CreatedAt DESC);
