-- Full change history for the data users maintain themselves.
--
-- System-versioned temporal tables make SQL Server copy the previous version of a row into a history table on
-- every UPDATE and DELETE (soft deletes are updates, so they are covered too). The application does not have
-- to remember to write an audit record, and it cannot forget to: the database does it in the same transaction.
-- "What did this card look like last March?" becomes: SELECT ... FOR SYSTEM_TIME AS OF '2026-03-01'.
--
-- The period columns are HIDDEN so existing explicit column lists and SELECT * stay unchanged.

ALTER TABLE finance.CreditCards ADD
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT DF_CreditCards_ValidFrom DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
        CONSTRAINT DF_CreditCards_ValidTo DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE finance.CreditCards
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = finance.CreditCardsHistory));

ALTER TABLE finance.RecurringCashFlows ADD
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT DF_RecurringCashFlows_ValidFrom DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
        CONSTRAINT DF_RecurringCashFlows_ValidTo DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE finance.RecurringCashFlows
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = finance.RecurringCashFlowsHistory));

ALTER TABLE finance.InstallmentPlans ADD
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT DF_InstallmentPlans_ValidFrom DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
        CONSTRAINT DF_InstallmentPlans_ValidTo DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE finance.InstallmentPlans
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = finance.InstallmentPlansHistory));
