IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(160) NOT NULL,
        [State] nvarchar(32) NOT NULL,
        [IsPlatformAdmin] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AuthenticationAudits] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Action] nvarchar(64) NOT NULL,
        [CorrelationId] nvarchar(64) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AuthenticationAudits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SecurityStamp] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSessions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AuthenticationAudits_CreatedAt] ON [AuthenticationAudits] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId_RevokedAt_ExpiresAt] ON [UserSessions] ([UserId], [RevokedAt], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108195751_InitialIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108195751_InitialIdentity', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE TABLE [Businesses] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Activity] nvarchar(160) NOT NULL,
        [Phone] nvarchar(32) NOT NULL,
        [CountryCode] varchar(2) NOT NULL,
        [TimeZoneId] nvarchar(100) NOT NULL,
        [WeekStartsOn] int NOT NULL,
        [SlotMinutes] int NOT NULL,
        [State] nvarchar(32) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Businesses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Businesses_SlotMinutes] CHECK ([SlotMinutes] IN (15, 30, 60)),
        CONSTRAINT [CK_Businesses_State] CHECK ([State] IN ('PendingApproval', 'Active', 'Rejected', 'Suspended', 'Archived')),
        CONSTRAINT [CK_Businesses_WeekStartsOn] CHECK ([WeekStartsOn] BETWEEN 0 AND 6)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE TABLE [AuditEvents] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [BusinessId] uniqueidentifier NULL,
        [Action] nvarchar(64) NOT NULL,
        [ResourceType] nvarchar(64) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [CorrelationId] nvarchar(64) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditEvents_AspNetUsers_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditEvents_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE TABLE [BusinessMemberships] (
        [BusinessId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Role] nvarchar(16) NOT NULL,
        [State] nvarchar(16) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_BusinessMemberships] PRIMARY KEY ([BusinessId], [UserId]),
        CONSTRAINT [CK_BusinessMemberships_RevokedAt] CHECK (([State] = 'Revoked' AND [RevokedAt] IS NOT NULL) OR ([State] <> 'Revoked' AND [RevokedAt] IS NULL)),
        CONSTRAINT [CK_BusinessMemberships_Role] CHECK ([Role] IN ('Owner', 'Manager')),
        CONSTRAINT [CK_BusinessMemberships_State] CHECK ([State] IN ('Pending', 'Active', 'Revoked')),
        CONSTRAINT [FK_BusinessMemberships_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BusinessMemberships_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessId] uniqueidentifier NULL,
        [Type] nvarchar(64) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [Recipient] nvarchar(256) NOT NULL,
        [DeduplicationKey] nvarchar(200) NOT NULL,
        [State] nvarchar(16) NOT NULL,
        [Attempts] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [NextAttemptAt] datetimeoffset NOT NULL,
        [LeaseId] uniqueidentifier NULL,
        [LeaseExpiresAt] datetimeoffset NULL,
        [SentAt] datetimeoffset NULL,
        [LastErrorCode] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OutboxMessages_Attempts] CHECK ([Attempts] >= 0),
        CONSTRAINT [CK_OutboxMessages_Lease] CHECK (([State] = 'Processing' AND [LeaseId] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL) OR ([State] <> 'Processing' AND [LeaseId] IS NULL AND [LeaseExpiresAt] IS NULL)),
        CONSTRAINT [CK_OutboxMessages_Payload] CHECK (ISJSON([Payload]) = 1 AND DATALENGTH([Payload]) <= 16000),
        CONSTRAINT [CK_OutboxMessages_SentAt] CHECK (([State] = 'Sent' AND [SentAt] IS NOT NULL) OR ([State] <> 'Sent' AND [SentAt] IS NULL)),
        CONSTRAINT [CK_OutboxMessages_State] CHECK ([State] IN ('Pending', 'Processing', 'Sent', 'Failed')),
        CONSTRAINT [FK_OutboxMessages_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE TABLE [RegistrationApplications] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessId] uniqueidentifier NOT NULL,
        [ApplicantUserId] uniqueidentifier NOT NULL,
        [State] nvarchar(32) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ReviewedBy] uniqueidentifier NULL,
        [ReviewedAt] datetimeoffset NULL,
        [PublicReason] nvarchar(1000) NULL,
        [InternalNote] nvarchar(2000) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RegistrationApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RegistrationApplications_RejectionReason] CHECK ([State] <> 'Rejected' OR ([PublicReason] IS NOT NULL AND LEN(LTRIM(RTRIM([PublicReason]))) > 0)),
        CONSTRAINT [CK_RegistrationApplications_Review] CHECK (([State] = 'PendingReview' AND [ReviewedBy] IS NULL AND [ReviewedAt] IS NULL) OR ([State] IN ('Approved', 'Rejected') AND [ReviewedBy] IS NOT NULL AND [ReviewedAt] IS NOT NULL)),
        CONSTRAINT [CK_RegistrationApplications_State] CHECK ([State] IN ('PendingReview', 'Approved', 'Rejected')),
        CONSTRAINT [FK_RegistrationApplications_AspNetUsers_ReviewedBy] FOREIGN KEY ([ReviewedBy]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RegistrationApplications_BusinessMemberships_BusinessId_ApplicantUserId] FOREIGN KEY ([BusinessId], [ApplicantUserId]) REFERENCES [BusinessMemberships] ([BusinessId], [UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_ActorUserId] ON [AuditEvents] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_BusinessId_CreatedAt] ON [AuditEvents] ([BusinessId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_CreatedAt] ON [AuditEvents] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_Businesses_State_CreatedAt] ON [Businesses] ([State], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_BusinessMemberships_UserId_State_BusinessId] ON [BusinessMemberships] ([UserId], [State], [BusinessId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_BusinessId] ON [OutboxMessages] ([BusinessId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OutboxMessages_DeduplicationKey] ON [OutboxMessages] ([DeduplicationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_State_LeaseExpiresAt] ON [OutboxMessages] ([State], [LeaseExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_State_NextAttemptAt] ON [OutboxMessages] ([State], [NextAttemptAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RegistrationApplications_ApplicantUserId] ON [RegistrationApplications] ([ApplicantUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RegistrationApplications_BusinessId] ON [RegistrationApplications] ([BusinessId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_RegistrationApplications_BusinessId_ApplicantUserId] ON [RegistrationApplications] ([BusinessId], [ApplicantUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_RegistrationApplications_ReviewedBy] ON [RegistrationApplications] ([ReviewedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    CREATE INDEX [IX_RegistrationApplications_State_CreatedAt] ON [RegistrationApplications] ([State], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108201531_BusinessFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108201531_BusinessFoundation', N'10.0.14');
END;

COMMIT;
GO

