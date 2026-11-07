-- Refresh tokens for sessions (docs/adr/0004).
--
-- Only a SHA-256 hash of each token is stored, so a copy of this table cannot be used to sign in. Tokens are
-- never updated in place: using one revokes it and records the token that replaced it. A revoked token that
-- shows up again was used twice, which means a copy exists somewhere it should not; the application then
-- revokes every session of that user.
--
-- Times come from the application (TimeProvider) rather than SYSUTCDATETIME(), so expiry is judged against the
-- same clock that issued it.

CREATE TABLE dbo.RefreshTokens
(
    TokenId            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
    UserId             BIGINT        NOT NULL CONSTRAINT FK_RefreshTokens_Users REFERENCES dbo.Users (UserId),
    TokenHash          BINARY(32)    NOT NULL,
    CreatedAt          DATETIME2(3)  NOT NULL,
    ExpiresAt          DATETIME2(3)  NOT NULL,
    RevokedAt          DATETIME2(3)  NULL,
    ReplacedByTokenId  BIGINT        NULL CONSTRAINT FK_RefreshTokens_ReplacedBy REFERENCES dbo.RefreshTokens (TokenId),

    -- Also the index every lookup by token seeks on.
    CONSTRAINT UQ_RefreshTokens_Hash UNIQUE (TokenHash),
    CONSTRAINT CK_RefreshTokens_Lifetime CHECK (ExpiresAt > CreatedAt)
);

-- "Revoke all of this user's live sessions" only ever looks at tokens that are still open.
CREATE INDEX IX_RefreshTokens_User_Open ON dbo.RefreshTokens (UserId) WHERE RevokedAt IS NULL;
