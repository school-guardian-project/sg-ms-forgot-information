using Microsoft.EntityFrameworkCore;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;

namespace ms_forgot_information.Api.Shared.Infrastructure.Persistence;

public static class VerificationRequestSchemaInitializer
{
    private const string Sql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        DECLARE @lockResult int;
        EXEC @lockResult = sys.sp_getapplock
            @Resource = N'ForgotInformation.VerificationRequest.Schema',
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = 15000;

        IF @lockResult < 0
            THROW 51000, 'Could not acquire the password recovery schema lock.', 1;

        IF SCHEMA_ID(N'ForgotInformation') IS NULL
            EXEC(N'CREATE SCHEMA [ForgotInformation]');

        IF OBJECT_ID(N'[ForgotInformation].[VerificationRequest]', N'U') IS NULL
        BEGIN
            CREATE TABLE [ForgotInformation].[VerificationRequest]
            (
                [Id] uniqueidentifier NOT NULL,
                [ProfileId] uniqueidentifier NOT NULL,
                [Purpose] varchar(20) NOT NULL,
                [Target] varchar(100) NOT NULL,
                [CodeHash] varchar(255) NOT NULL,
                [ResetTokenHash] varchar(255) NULL,
                [ExpiresAt] datetime2 NOT NULL,
                [AttemptCount] tinyint NOT NULL DEFAULT 0,
                [MaxAttempts] tinyint NOT NULL DEFAULT 5,
                [Status] varchar(20) NOT NULL CHECK (Status IN ('Pending', 'Verified', 'Consumed', 'Expired', 'Locked')) DEFAULT 'Pending',
                [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                [VerifiedAt] datetime2 NULL,
                [ConsumedAt] datetime2 NULL,
                [RequestIp] varchar(50) NOT NULL,
                CONSTRAINT [PK_VerificationRequest] PRIMARY KEY ([Id])
            );
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N'[ForgotInformation].[VerificationRequest]')
                AND [name] = N'idx_verificationrequest_profile_purpose_status'
        )
            CREATE INDEX [idx_verificationrequest_profile_purpose_status]
                ON [ForgotInformation].[VerificationRequest] ([ProfileId], [Purpose], [Status]);

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N'[ForgotInformation].[VerificationRequest]')
                AND [name] = N'idx_verificationrequest_expiresat'
        )
            CREATE INDEX [idx_verificationrequest_expiresat]
                ON [ForgotInformation].[VerificationRequest] ([ExpiresAt]);

        COMMIT TRANSACTION;
        """;

    public static Task InitializeAsync(ForgotInformationContext context, CancellationToken cancellationToken = default)
    {
        return context.Database.ExecuteSqlRawAsync(Sql, cancellationToken);
    }
}
