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
CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [IsApproved] bit NOT NULL,
    [RoleId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserRegistrationRequests] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [RequestedRoleId] int NOT NULL,
    [IsApproved] bit NOT NULL,
    [ApprovedByUserId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ApprovedAt] datetime2 NULL,
    CONSTRAINT [PK_UserRegistrationRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRegistrationRequests_Roles_RequestedRoleId] FOREIGN KEY ([RequestedRoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRegistrationRequests_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [CreatedAt], [Name])
VALUES (1, '2026-02-11T00:00:00.0000000Z', N'Yönetici'),
(2, '2026-02-11T00:00:00.0000000Z', N'Denetleyici'),
(3, '2026-02-11T00:00:00.0000000Z', N'Görüntüleyici');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);

CREATE INDEX [IX_UserRegistrationRequests_ApprovedByUserId] ON [UserRegistrationRequests] ([ApprovedByUserId]);

CREATE UNIQUE INDEX [IX_UserRegistrationRequests_Email] ON [UserRegistrationRequests] ([Email]);

CREATE INDEX [IX_UserRegistrationRequests_RequestedRoleId] ON [UserRegistrationRequests] ([RequestedRoleId]);

CREATE UNIQUE INDEX [IX_UserRegistrationRequests_Username] ON [UserRegistrationRequests] ([Username]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260211105249_InitialCreate', N'9.0.13');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserRegistrationRequests]') AND [c].[name] = N'IsApproved');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [UserRegistrationRequests] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [UserRegistrationRequests] DROP COLUMN [IsApproved];

ALTER TABLE [UserRegistrationRequests] ADD [RejectedAt] datetime2 NULL;

ALTER TABLE [UserRegistrationRequests] ADD [RejectionReason] nvarchar(max) NULL;

ALTER TABLE [UserRegistrationRequests] ADD [Status] int NOT NULL DEFAULT 0;

CREATE TABLE [PasswordSetupTokens] (
    [Id] int NOT NULL IDENTITY,
    [RegistrationRequestId] int NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    [UsedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PasswordSetupTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordSetupTokens_UserRegistrationRequests_RegistrationRequestId] FOREIGN KEY ([RegistrationRequestId]) REFERENCES [UserRegistrationRequests] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_UserRegistrationRequests_Status] ON [UserRegistrationRequests] ([Status]);

CREATE INDEX [IX_PasswordSetupTokens_RegistrationRequestId] ON [PasswordSetupTokens] ([RegistrationRequestId]);

CREATE UNIQUE INDEX [IX_PasswordSetupTokens_TokenHash] ON [PasswordSetupTokens] ([TokenHash]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260211120711_AddPasswordSetupTokenAndStatus', N'9.0.13');

DROP INDEX [IX_UserRegistrationRequests_Email] ON [UserRegistrationRequests];

DROP INDEX [IX_UserRegistrationRequests_Username] ON [UserRegistrationRequests];

CREATE UNIQUE INDEX [IX_UserRegistrationRequests_Email] ON [UserRegistrationRequests] ([Email]) WHERE [Status] = 0;

CREATE UNIQUE INDEX [IX_UserRegistrationRequests_Username] ON [UserRegistrationRequests] ([Username]) WHERE [Status] = 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260212074053_FixUniqueIndexesForRegistrationRequests', N'9.0.13');

CREATE TABLE [Computers] (
    [Id] int NOT NULL IDENTITY,
    [MacAddress] nvarchar(17) NOT NULL,
    [MachineName] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(100) NULL,
    [IpAddress] nvarchar(50) NULL,
    [CpuModel] nvarchar(200) NULL,
    [TotalRamMb] float NOT NULL,
    [TotalDiskGb] float NOT NULL,
    [LastSeen] datetime2 NOT NULL,
    CONSTRAINT [PK_Computers] PRIMARY KEY ([Id])
);

CREATE TABLE [ComputerMetrics] (
    [Id] bigint NOT NULL IDENTITY,
    [ComputerId] int NOT NULL,
    [CpuUsage] float NOT NULL,
    [RamUsage] float NOT NULL,
    [DiskUsage] float NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ComputerMetrics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComputerMetrics_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ComputerMetrics_ComputerId] ON [ComputerMetrics] ([ComputerId]);

CREATE UNIQUE INDEX [IX_Computers_MacAddress] ON [Computers] ([MacAddress]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260213125516_AddComputerTelemetry', N'9.0.13');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'TotalDiskGb');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Computers] ALTER COLUMN [TotalDiskGb] nvarchar(max) NULL;

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'MachineName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Computers] ALTER COLUMN [MachineName] nvarchar(max) NOT NULL;

DROP INDEX [IX_Computers_MacAddress] ON [Computers];
DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'MacAddress');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Computers] ALTER COLUMN [MacAddress] nvarchar(450) NOT NULL;
CREATE UNIQUE INDEX [IX_Computers_MacAddress] ON [Computers] ([MacAddress]);

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'IpAddress');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Computers] ALTER COLUMN [IpAddress] nvarchar(max) NULL;

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'DisplayName');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Computers] ALTER COLUMN [DisplayName] nvarchar(max) NULL;

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'CpuModel');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Computers] ALTER COLUMN [CpuModel] nvarchar(max) NULL;

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerMetrics]') AND [c].[name] = N'DiskUsage');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ComputerMetrics] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [ComputerMetrics] ALTER COLUMN [DiskUsage] nvarchar(max) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260213133411_ChangeDiskFieldsToString', N'9.0.13');

ALTER TABLE [Computers] ADD [LastNotifyTime] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260213141834_AddLastNotifyTime', N'9.0.13');

ALTER TABLE [PasswordSetupTokens] DROP CONSTRAINT [FK_PasswordSetupTokens_UserRegistrationRequests_RegistrationRequestId];

ALTER TABLE [UserRegistrationRequests] DROP CONSTRAINT [FK_UserRegistrationRequests_Roles_RequestedRoleId];

ALTER TABLE [UserRegistrationRequests] DROP CONSTRAINT [FK_UserRegistrationRequests_Users_ApprovedByUserId];

ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Roles_RoleId];

DROP INDEX [IX_Users_Email] ON [Users];

DROP INDEX [IX_Roles_Name] ON [Roles];

DROP INDEX [IX_PasswordSetupTokens_TokenHash] ON [PasswordSetupTokens];

ALTER TABLE [UserRegistrationRequests] DROP CONSTRAINT [PK_UserRegistrationRequests];

DROP INDEX [IX_UserRegistrationRequests_Email] ON [UserRegistrationRequests];

DROP INDEX [IX_UserRegistrationRequests_Status] ON [UserRegistrationRequests];

DROP INDEX [IX_UserRegistrationRequests_Username] ON [UserRegistrationRequests];

DELETE FROM [Roles]
WHERE [Id] = 1;
SELECT @@ROWCOUNT;


DELETE FROM [Roles]
WHERE [Id] = 2;
SELECT @@ROWCOUNT;


DELETE FROM [Roles]
WHERE [Id] = 3;
SELECT @@ROWCOUNT;


EXEC sp_rename N'[UserRegistrationRequests]', N'RegistrationRequests', 'OBJECT';

EXEC sp_rename N'[RegistrationRequests].[IX_UserRegistrationRequests_RequestedRoleId]', N'IX_RegistrationRequests_RequestedRoleId', 'INDEX';

EXEC sp_rename N'[RegistrationRequests].[IX_UserRegistrationRequests_ApprovedByUserId]', N'IX_RegistrationRequests_ApprovedByUserId', 'INDEX';

DROP INDEX [IX_Users_Username] ON [Users];
DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Username');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Users] ALTER COLUMN [Username] nvarchar(450) NOT NULL;
CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Users] ALTER COLUMN [PasswordHash] nvarchar(max) NOT NULL;

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Email');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(max) NOT NULL;

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'Name');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Roles] ALTER COLUMN [Name] nvarchar(max) NOT NULL;

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PasswordSetupTokens]') AND [c].[name] = N'TokenHash');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [PasswordSetupTokens] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [PasswordSetupTokens] ALTER COLUMN [TokenHash] nvarchar(max) NOT NULL;

DROP INDEX [IX_Computers_MacAddress] ON [Computers];
DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'MacAddress');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Computers] ALTER COLUMN [MacAddress] nvarchar(50) NOT NULL;
CREATE UNIQUE INDEX [IX_Computers_MacAddress] ON [Computers] ([MacAddress]);

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'Username');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [RegistrationRequests] ALTER COLUMN [Username] nvarchar(450) NOT NULL;

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'Email');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [RegistrationRequests] ALTER COLUMN [Email] nvarchar(450) NOT NULL;

ALTER TABLE [RegistrationRequests] ADD CONSTRAINT [PK_RegistrationRequests] PRIMARY KEY ([Id]);

CREATE INDEX [IX_ComputerMetrics_CreatedAt] ON [ComputerMetrics] ([CreatedAt]);

CREATE UNIQUE INDEX [IX_RegistrationRequests_Email] ON [RegistrationRequests] ([Email]);

CREATE UNIQUE INDEX [IX_RegistrationRequests_Username] ON [RegistrationRequests] ([Username]);

ALTER TABLE [PasswordSetupTokens] ADD CONSTRAINT [FK_PasswordSetupTokens_RegistrationRequests_RegistrationRequestId] FOREIGN KEY ([RegistrationRequestId]) REFERENCES [RegistrationRequests] ([Id]) ON DELETE CASCADE;

ALTER TABLE [RegistrationRequests] ADD CONSTRAINT [FK_RegistrationRequests_Roles_RequestedRoleId] FOREIGN KEY ([RequestedRoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE;

ALTER TABLE [RegistrationRequests] ADD CONSTRAINT [FK_RegistrationRequests_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]);

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260216085623_PerformanceKeys', N'9.0.13');

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'TotalDiskGb');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [Computers] DROP COLUMN [TotalDiskGb];

ALTER TABLE [Computers] ADD [CpuThreshold] float NOT NULL DEFAULT 0.0E0;

ALTER TABLE [Computers] ADD [RamThreshold] float NOT NULL DEFAULT 0.0E0;

CREATE TABLE [ComputerDisks] (
    [Id] int NOT NULL IDENTITY,
    [ComputerId] int NOT NULL,
    [DiskName] nvarchar(max) NOT NULL,
    [TotalSizeGb] float NOT NULL,
    [ThresholdPercent] float NOT NULL,
    CONSTRAINT [PK_ComputerDisks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComputerDisks_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DiskMetrics] (
    [Id] bigint NOT NULL IDENTITY,
    [ComputerDiskId] int NOT NULL,
    [UsedPercent] float NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DiskMetrics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiskMetrics_ComputerDisks_ComputerDiskId] FOREIGN KEY ([ComputerDiskId]) REFERENCES [ComputerDisks] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ComputerDisks_ComputerId] ON [ComputerDisks] ([ComputerId]);

CREATE INDEX [IX_DiskMetrics_ComputerDiskId] ON [DiskMetrics] ([ComputerDiskId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260216113348_AddThresholdsAndDiskTables', N'9.0.13');

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerMetrics]') AND [c].[name] = N'DiskUsage');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [ComputerMetrics] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [ComputerMetrics] DROP COLUMN [DiskUsage];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260216115925_RemoveOldDiskUsageField', N'9.0.13');

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'RamThreshold');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [Computers] ALTER COLUMN [RamThreshold] float NULL;

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'CpuThreshold');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [Computers] ALTER COLUMN [CpuThreshold] float NULL;

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerDisks]') AND [c].[name] = N'ThresholdPercent');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [ComputerDisks] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [ComputerDisks] ALTER COLUMN [ThresholdPercent] float NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260216123226_NullableThresholds', N'9.0.13');

CREATE TABLE [Tags] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
);

CREATE TABLE [ComputerTags] (
    [ComputersId] int NOT NULL,
    [TagsId] int NOT NULL,
    CONSTRAINT [PK_ComputerTags] PRIMARY KEY ([ComputersId], [TagsId]),
    CONSTRAINT [FK_ComputerTags_Computers_ComputersId] FOREIGN KEY ([ComputersId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ComputerTags_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ComputerTags_TagsId] ON [ComputerTags] ([TagsId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260216133826_AddTaggingSystem', N'9.0.13');

ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Roles_RoleId];

DROP INDEX [IX_Users_RoleId] ON [Users];

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'RoleId');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [Users] DROP COLUMN [RoleId];

CREATE TABLE [UserRoles] (
    [RolesId] int NOT NULL,
    [UsersId] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([RolesId], [UsersId]),
    CONSTRAINT [FK_UserRoles_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_UserRoles_UsersId] ON [UserRoles] ([UsersId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260217080154_MultiRoleSupport', N'9.0.13');

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [Users] ALTER COLUMN [PasswordHash] nvarchar(200) NOT NULL;

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Email');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var23 + '];');
ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(200) NOT NULL;

DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tags]') AND [c].[name] = N'Name');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Tags] DROP CONSTRAINT [' + @var24 + '];');
ALTER TABLE [Tags] ALTER COLUMN [Name] nvarchar(200) NOT NULL;

DECLARE @var25 sysname;
SELECT @var25 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'Name');
IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var25 + '];');
ALTER TABLE [Roles] ALTER COLUMN [Name] nvarchar(200) NOT NULL;

DECLARE @var26 sysname;
SELECT @var26 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'RejectionReason');
IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var26 + '];');
ALTER TABLE [RegistrationRequests] ALTER COLUMN [RejectionReason] nvarchar(200) NULL;

DECLARE @var27 sysname;
SELECT @var27 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PasswordSetupTokens]') AND [c].[name] = N'TokenHash');
IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [PasswordSetupTokens] DROP CONSTRAINT [' + @var27 + '];');
ALTER TABLE [PasswordSetupTokens] ALTER COLUMN [TokenHash] nvarchar(200) NOT NULL;

DECLARE @var28 sysname;
SELECT @var28 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'MachineName');
IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var28 + '];');
ALTER TABLE [Computers] ALTER COLUMN [MachineName] nvarchar(200) NOT NULL;

DECLARE @var29 sysname;
SELECT @var29 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'IpAddress');
IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var29 + '];');
ALTER TABLE [Computers] ALTER COLUMN [IpAddress] nvarchar(200) NULL;

DECLARE @var30 sysname;
SELECT @var30 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'DisplayName');
IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var30 + '];');
ALTER TABLE [Computers] ALTER COLUMN [DisplayName] nvarchar(200) NULL;

DECLARE @var31 sysname;
SELECT @var31 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'CpuModel');
IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var31 + '];');
ALTER TABLE [Computers] ALTER COLUMN [CpuModel] nvarchar(200) NULL;

DECLARE @var32 sysname;
SELECT @var32 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerDisks]') AND [c].[name] = N'DiskName');
IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [ComputerDisks] DROP CONSTRAINT [' + @var32 + '];');
ALTER TABLE [ComputerDisks] ALTER COLUMN [DiskName] nvarchar(200) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260218072803_LimitStringLengthsTo200', N'9.0.13');

DROP INDEX [IX_RegistrationRequests_Email] ON [RegistrationRequests];

DROP INDEX [IX_RegistrationRequests_Username] ON [RegistrationRequests];

CREATE INDEX [IX_RegistrationRequests_Email] ON [RegistrationRequests] ([Email]);

CREATE INDEX [IX_RegistrationRequests_Username] ON [RegistrationRequests] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260218082012_AllowMultipleRequests', N'9.0.13');

EXEC sp_rename N'[Computers].[LastNotifyTime]', N'RamLastNotifyTime', 'COLUMN';

ALTER TABLE [Computers] ADD [CpuLastNotifyTime] datetime2 NULL;

ALTER TABLE [ComputerDisks] ADD [LastNotifyTime] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260218132205_SplitNotificationTimes', N'9.0.13');

DROP INDEX [IX_Users_Username] ON [Users];

DROP INDEX [IX_Computers_MacAddress] ON [Computers];

ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Tags] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [RegistrationRequests] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Computers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE INDEX [IX_Users_Username] ON [Users] ([Username]);

CREATE INDEX [IX_Computers_MacAddress] ON [Computers] ([MacAddress]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260219063119_AddSoftDeleteAndRemoveUniqueIndexes', N'9.0.13');

DECLARE @var33 sysname;
SELECT @var33 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'IsDeleted');
IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var33 + '];');
ALTER TABLE [RegistrationRequests] DROP COLUMN [IsDeleted];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260219064128_RemoveIsDeletedFromRegistrationRequests', N'9.0.13');

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [RoleComputerAccesses] (
    [RoleId] int NOT NULL,
    [ComputerId] int NOT NULL,
    CONSTRAINT [PK_RoleComputerAccesses] PRIMARY KEY ([RoleId], [ComputerId]),
    CONSTRAINT [FK_RoleComputerAccesses_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleComputerAccesses_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RoleTagAccesses] (
    [RoleId] int NOT NULL,
    [TagId] int NOT NULL,
    CONSTRAINT [PK_RoleTagAccesses] PRIMARY KEY ([RoleId], [TagId]),
    CONSTRAINT [FK_RoleTagAccesses_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleTagAccesses_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);

CREATE INDEX [IX_RoleComputerAccesses_ComputerId] ON [RoleComputerAccesses] ([ComputerId]);

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);

CREATE INDEX [IX_RoleTagAccesses_TagId] ON [RoleTagAccesses] ([TagId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260224075011_AddDynamicRoleSystem', N'9.0.13');

DROP TABLE [RoleComputerAccesses];

DROP TABLE [RoleTagAccesses];

CREATE TABLE [UserComputerAccesses] (
    [UserId] int NOT NULL,
    [ComputerId] int NOT NULL,
    CONSTRAINT [PK_UserComputerAccesses] PRIMARY KEY ([UserId], [ComputerId]),
    CONSTRAINT [FK_UserComputerAccesses_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserComputerAccesses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserTagAccesses] (
    [UserId] int NOT NULL,
    [TagId] int NOT NULL,
    CONSTRAINT [PK_UserTagAccesses] PRIMARY KEY ([UserId], [TagId]),
    CONSTRAINT [FK_UserTagAccesses_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserTagAccesses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_UserComputerAccesses_ComputerId] ON [UserComputerAccesses] ([ComputerId]);

CREATE INDEX [IX_UserTagAccesses_TagId] ON [UserTagAccesses] ([TagId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260225124605_UserAccessControl', N'9.0.13');

ALTER TABLE [Users] ADD [CreatedBy] int NULL;

ALTER TABLE [Users] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Users] ADD [DeletedBy] int NULL;

ALTER TABLE [Users] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [Users] ADD [UpdatedBy] int NULL;

ALTER TABLE [Computers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Computers] ADD [CreatedBy] int NULL;

ALTER TABLE [Computers] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Computers] ADD [DeletedBy] int NULL;

ALTER TABLE [Computers] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [Computers] ADD [UpdatedBy] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303063109_AddAuditLoggingFields', N'9.0.13');

ALTER TABLE [ComputerTags] DROP CONSTRAINT [FK_ComputerTags_Computers_ComputersId];

ALTER TABLE [ComputerTags] DROP CONSTRAINT [FK_ComputerTags_Tags_TagsId];

ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [FK_RegistrationRequests_Roles_RequestedRoleId];

ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [FK_RegistrationRequests_Users_ApprovedByUserId];

ALTER TABLE [UserRoles] DROP CONSTRAINT [FK_UserRoles_Roles_RolesId];

ALTER TABLE [UserRoles] DROP CONSTRAINT [FK_UserRoles_Users_UsersId];

DROP INDEX [IX_RegistrationRequests_ApprovedByUserId] ON [RegistrationRequests];

DROP INDEX [IX_RegistrationRequests_RequestedRoleId] ON [RegistrationRequests];

DECLARE @var34 sysname;
SELECT @var34 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CreatedBy');
IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var34 + '];');
ALTER TABLE [Users] DROP COLUMN [CreatedBy];

DECLARE @var35 sysname;
SELECT @var35 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'DeletedAt');
IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var35 + '];');
ALTER TABLE [Users] DROP COLUMN [DeletedAt];

DECLARE @var36 sysname;
SELECT @var36 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'DeletedBy');
IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var36 + '];');
ALTER TABLE [Users] DROP COLUMN [DeletedBy];

DECLARE @var37 sysname;
SELECT @var37 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UpdatedAt');
IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var37 + '];');
ALTER TABLE [Users] DROP COLUMN [UpdatedAt];

DECLARE @var38 sysname;
SELECT @var38 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UpdatedBy');
IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var38 + '];');
ALTER TABLE [Users] DROP COLUMN [UpdatedBy];

DECLARE @var39 sysname;
SELECT @var39 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'CreatedBy');
IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var39 + '];');
ALTER TABLE [Computers] DROP COLUMN [CreatedBy];

DECLARE @var40 sysname;
SELECT @var40 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'DeletedAt');
IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var40 + '];');
ALTER TABLE [Computers] DROP COLUMN [DeletedAt];

DECLARE @var41 sysname;
SELECT @var41 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'DeletedBy');
IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var41 + '];');
ALTER TABLE [Computers] DROP COLUMN [DeletedBy];

DECLARE @var42 sysname;
SELECT @var42 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'UpdatedAt');
IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var42 + '];');
ALTER TABLE [Computers] DROP COLUMN [UpdatedAt];

DECLARE @var43 sysname;
SELECT @var43 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Computers]') AND [c].[name] = N'UpdatedBy');
IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [Computers] DROP CONSTRAINT [' + @var43 + '];');
ALTER TABLE [Computers] DROP COLUMN [UpdatedBy];

EXEC sp_rename N'[UserRoles].[UsersId]', N'RoleId', 'COLUMN';

EXEC sp_rename N'[UserRoles].[RolesId]', N'UserId', 'COLUMN';

EXEC sp_rename N'[UserRoles].[IX_UserRoles_UsersId]', N'IX_UserRoles_RoleId', 'INDEX';

EXEC sp_rename N'[ComputerTags].[TagsId]', N'TagId', 'COLUMN';

EXEC sp_rename N'[ComputerTags].[ComputersId]', N'ComputerId', 'COLUMN';

EXEC sp_rename N'[ComputerTags].[IX_ComputerTags_TagsId]', N'IX_ComputerTags_TagId', 'INDEX';

ALTER TABLE [UserRoles] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [UserRoles] ADD [CreatedBy] int NULL;

ALTER TABLE [UserRoles] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [UserRoles] ADD [DeletedBy] int NULL;

ALTER TABLE [UserRoles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Tags] ADD [ColorHex] nvarchar(max) NULL;

ALTER TABLE [Tags] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Tags] ADD [CreatedBy] int NULL;

ALTER TABLE [Tags] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Tags] ADD [DeletedBy] int NULL;

ALTER TABLE [Tags] ADD [Description] nvarchar(max) NULL;

DECLARE @var44 sysname;
SELECT @var44 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'RequestedRoleId');
IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var44 + '];');
ALTER TABLE [RegistrationRequests] ALTER COLUMN [RequestedRoleId] int NULL;

ALTER TABLE [RegistrationRequests] ADD [FirstName] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [RegistrationRequests] ADD [LastName] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [RegistrationRequests] ADD [PasswordHash] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [RegistrationRequests] ADD [ProcessedDate] datetime2 NULL;

ALTER TABLE [RegistrationRequests] ADD [RejectedBy] int NULL;

ALTER TABLE [RegistrationRequests] ADD [RequestDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [ComputerTags] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [ComputerTags] ADD [CreatedBy] int NULL;

ALTER TABLE [ComputerTags] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [ComputerTags] ADD [DeletedBy] int NULL;

ALTER TABLE [ComputerTags] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [ComputerDisks] ADD [FreeSpaceThresholdGb] float NULL;

ALTER TABLE [ComputerDisks] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [ComputerDisks] ADD [UpdatedBy] int NULL;

ALTER TABLE [ComputerTags] ADD CONSTRAINT [FK_ComputerTags_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE;

DELETE FROM ComputerTags WHERE TagId NOT IN (SELECT Id FROM Tags)

DELETE FROM ComputerTags WHERE ComputerId NOT IN (SELECT Id FROM Computers)

ALTER TABLE [ComputerTags] ADD CONSTRAINT [FK_ComputerTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE;

DELETE FROM UserRoles WHERE RoleId NOT IN (SELECT Id FROM Roles)

DELETE FROM UserRoles WHERE UserId NOT IN (SELECT Id FROM Users)

ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE;

ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303084640_ApplyInterfaceAuditLogs', N'9.0.13');

ALTER TABLE [UserRoles] DROP CONSTRAINT [PK_UserRoles];

ALTER TABLE [ComputerTags] DROP CONSTRAINT [PK_ComputerTags];

ALTER TABLE [UserRoles] ADD [Id] int NOT NULL IDENTITY;

ALTER TABLE [ComputerTags] ADD [Id] int NOT NULL IDENTITY;

ALTER TABLE [UserRoles] ADD CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]);

ALTER TABLE [ComputerTags] ADD CONSTRAINT [PK_ComputerTags] PRIMARY KEY ([Id]);

CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);

CREATE INDEX [IX_ComputerTags_ComputerId] ON [ComputerTags] ([ComputerId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303101859_FixJunctionTablePK', N'9.0.13');

DECLARE @var45 sysname;
SELECT @var45 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'FirstName');
IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var45 + '];');
ALTER TABLE [RegistrationRequests] DROP COLUMN [FirstName];

DECLARE @var46 sysname;
SELECT @var46 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'LastName');
IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var46 + '];');
ALTER TABLE [RegistrationRequests] DROP COLUMN [LastName];

DECLARE @var47 sysname;
SELECT @var47 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'ProcessedDate');
IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var47 + '];');
ALTER TABLE [RegistrationRequests] DROP COLUMN [ProcessedDate];

DECLARE @var48 sysname;
SELECT @var48 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'RequestDate');
IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var48 + '];');
ALTER TABLE [RegistrationRequests] DROP COLUMN [RequestDate];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303103102_RemoveNamesFromRegistration', N'9.0.13');

DECLARE @var49 sysname;
SELECT @var49 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RegistrationRequests]') AND [c].[name] = N'PasswordHash');
IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [RegistrationRequests] DROP CONSTRAINT [' + @var49 + '];');
ALTER TABLE [RegistrationRequests] DROP COLUMN [PasswordHash];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303103508_RemovePasswordFromRegistration', N'9.0.13');

DECLARE @var50 sysname;
SELECT @var50 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tags]') AND [c].[name] = N'ColorHex');
IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [Tags] DROP CONSTRAINT [' + @var50 + '];');
ALTER TABLE [Tags] DROP COLUMN [ColorHex];

DECLARE @var51 sysname;
SELECT @var51 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tags]') AND [c].[name] = N'Description');
IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [Tags] DROP CONSTRAINT [' + @var51 + '];');
ALTER TABLE [Tags] DROP COLUMN [Description];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303104609_RemoveTagExtras', N'9.0.13');

DECLARE @var52 sysname;
SELECT @var52 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerDisks]') AND [c].[name] = N'FreeSpaceThresholdGb');
IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [ComputerDisks] DROP CONSTRAINT [' + @var52 + '];');
ALTER TABLE [ComputerDisks] DROP COLUMN [FreeSpaceThresholdGb];

ALTER TABLE [Users] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Users] ADD [DeletedBy] int NULL;

ALTER TABLE [Computers] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [Computers] ADD [UpdatedBy] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303115638_FinalDatabaseCleanup', N'9.0.13');

CREATE TABLE [SidebarItems] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Icon] nvarchar(max) NULL,
    [TargetView] nvarchar(max) NOT NULL,
    [RequiredPermission] nvarchar(max) NOT NULL,
    [OrderIndex] int NOT NULL,
    CONSTRAINT [PK_SidebarItems] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305122623_AddSidebarItems', N'9.0.13');

DECLARE @var53 sysname;
SELECT @var53 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SidebarItems]') AND [c].[name] = N'RequiredPermission');
IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [SidebarItems] DROP CONSTRAINT [' + @var53 + '];');
ALTER TABLE [SidebarItems] ALTER COLUMN [RequiredPermission] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305123018_MakeRequiredPermissionNullable', N'9.0.13');

CREATE TABLE [UserTableActions] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Icon] nvarchar(max) NOT NULL,
    [ButtonClass] nvarchar(max) NOT NULL,
    [OnClickFunction] nvarchar(max) NOT NULL,
    [RequiredPermission] nvarchar(max) NULL,
    [OrderIndex] int NOT NULL,
    CONSTRAINT [PK_UserTableActions] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316082840_AddDynamicUserActionsTable', N'9.0.13');

ALTER TABLE [UserRoles] ADD [RoleId1] int NULL;

ALTER TABLE [Roles] ADD [CreatedBy] int NULL;

ALTER TABLE [Roles] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Roles] ADD [DeletedBy] int NULL;

ALTER TABLE [Roles] ADD [Description] nvarchar(max) NULL;

ALTER TABLE [Roles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Roles] ADD [UpdatedAt] datetime2 NULL;

ALTER TABLE [Roles] ADD [UpdatedBy] int NULL;

CREATE INDEX [IX_UserRoles_RoleId1] ON [UserRoles] ([RoleId1]);

ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Roles_RoleId1] FOREIGN KEY ([RoleId1]) REFERENCES [Roles] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316195823_UpdateRoleTableForAuditAndSoftDelete', N'9.0.13');

CREATE INDEX [IX_DiskMetrics_CreatedAt] ON [DiskMetrics] ([CreatedAt]);

CREATE INDEX [IX_ComputerMetrics_ComputerId_CreatedAt] ON [ComputerMetrics] ([ComputerId], [CreatedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260317140905_AddMetricsIndexes', N'9.0.13');

DROP INDEX [IX_DiskMetrics_ComputerDiskId] ON [DiskMetrics];

DROP INDEX [IX_DiskMetrics_CreatedAt] ON [DiskMetrics];

DROP INDEX [IX_ComputerMetrics_CreatedAt] ON [ComputerMetrics];

CREATE INDEX [IX_DiskMetrics_ComputerDiskId_CreatedAt] ON [DiskMetrics] ([ComputerDiskId], [CreatedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318123003_FixMetricIndexes', N'9.0.13');

CREATE TABLE [EndpointPermissions] (
    [Id] int NOT NULL IDENTITY,
    [ControllerName] nvarchar(max) NOT NULL,
    [ActionName] nvarchar(max) NOT NULL,
    [RequiredPermission] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_EndpointPermissions] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323073312_AddEndpointPermissionsTable', N'9.0.13');

DROP TABLE [EndpointPermissions];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323133454_RemoveEndpointPermissions', N'9.0.13');

DECLARE @var54 sysname;
SELECT @var54 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SidebarItems]') AND [c].[name] = N'RequiredPermission');
IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [SidebarItems] DROP CONSTRAINT [' + @var54 + '];');
ALTER TABLE [SidebarItems] DROP COLUMN [RequiredPermission];

ALTER TABLE [SidebarItems] ADD [RequiredPermissionId] int NULL;

CREATE INDEX [IX_SidebarItems_RequiredPermissionId] ON [SidebarItems] ([RequiredPermissionId]);

ALTER TABLE [SidebarItems] ADD CONSTRAINT [FK_SidebarItems_Permissions_RequiredPermissionId] FOREIGN KEY ([RequiredPermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323141201_UpdateSidebarPermissionRelation', N'9.0.13');

ALTER TABLE [SidebarItems] DROP CONSTRAINT [FK_SidebarItems_Permissions_RequiredPermissionId];

DROP INDEX [IX_SidebarItems_RequiredPermissionId] ON [SidebarItems];

DROP INDEX [IX_Permissions_Name] ON [Permissions];

DECLARE @var55 sysname;
SELECT @var55 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SidebarItems]') AND [c].[name] = N'RequiredPermissionId');
IF @var55 IS NOT NULL EXEC(N'ALTER TABLE [SidebarItems] DROP CONSTRAINT [' + @var55 + '];');
ALTER TABLE [SidebarItems] DROP COLUMN [RequiredPermissionId];

DECLARE @var56 sysname;
SELECT @var56 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Permissions]') AND [c].[name] = N'Description');
IF @var56 IS NOT NULL EXEC(N'ALTER TABLE [Permissions] DROP CONSTRAINT [' + @var56 + '];');
ALTER TABLE [Permissions] DROP COLUMN [Description];

DECLARE @var57 sysname;
SELECT @var57 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Permissions]') AND [c].[name] = N'Name');
IF @var57 IS NOT NULL EXEC(N'ALTER TABLE [Permissions] DROP CONSTRAINT [' + @var57 + '];');
ALTER TABLE [Permissions] ALTER COLUMN [Name] nvarchar(max) NOT NULL;

ALTER TABLE [Permissions] ADD [SidebarItemId] int NULL;

CREATE INDEX [IX_Permissions_SidebarItemId] ON [Permissions] ([SidebarItemId]);

ALTER TABLE [Permissions] ADD CONSTRAINT [FK_Permissions_SidebarItems_SidebarItemId] FOREIGN KEY ([SidebarItemId]) REFERENCES [SidebarItems] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260324112512_RemoveDescriptionAndReverseSidebar', N'9.0.13');

DECLARE @var58 sysname;
SELECT @var58 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Permissions]') AND [c].[name] = N'Name');
IF @var58 IS NOT NULL EXEC(N'ALTER TABLE [Permissions] DROP CONSTRAINT [' + @var58 + '];');
ALTER TABLE [Permissions] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

ALTER TABLE [Permissions] ADD [Description] nvarchar(200) NOT NULL DEFAULT N'';

CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260324113102_UpdatePermissionAndSidebarRelation', N'9.0.13');

DROP TABLE [UserTableActions];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260324125037_RemoveUserTableAction', N'9.0.13');

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [Token] nvarchar(max) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260330141429_AddRefreshTokenTable', N'9.0.13');

ALTER TABLE [Computers] ADD [IsOfflineAlertSent] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260401135550_AddOfflineAlertFlag', N'9.0.13');

ALTER TABLE [UserTagAccesses] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [UserTagAccesses] ADD [DeletedBy] int NULL;

ALTER TABLE [UserTagAccesses] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [UserComputerAccesses] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [UserComputerAccesses] ADD [DeletedBy] int NULL;

ALTER TABLE [UserComputerAccesses] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [RolePermissions] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [RolePermissions] ADD [DeletedBy] int NULL;

ALTER TABLE [RolePermissions] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260402131158_AddSoftDeleteToJunctionTables', N'9.0.13');

DROP INDEX [IX_Users_Username] ON [Users];

DROP INDEX [IX_Computers_MacAddress] ON [Computers];

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [IsDeleted] = 0;

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]) WHERE [IsDeleted] = 0;

CREATE UNIQUE INDEX [IX_Tags_Name] ON [Tags] ([Name]) WHERE [IsDeleted] = 0;

DROP INDEX [IX_Roles_Name] ON [Roles];

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]) WHERE [IsDeleted] = 0;

CREATE UNIQUE INDEX [IX_Computers_MacAddress] ON [Computers] ([MacAddress]) WHERE [IsDeleted] = 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260409124347_AddFilteredUniqueConstraints', N'9.0.13');

CREATE TABLE [ComputerThresholdHistories] (
    [Id] int NOT NULL IDENTITY,
    [ComputerId] int NOT NULL,
    [CpuThreshold] float NULL,
    [RamThreshold] float NULL,
    [ActiveFrom] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    CONSTRAINT [PK_ComputerThresholdHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComputerThresholdHistories_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DiskThresholdHistories] (
    [Id] int NOT NULL IDENTITY,
    [ComputerDiskId] int NOT NULL,
    [ThresholdPercent] float NULL,
    [ActiveFrom] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    CONSTRAINT [PK_DiskThresholdHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiskThresholdHistories_ComputerDisks_ComputerDiskId] FOREIGN KEY ([ComputerDiskId]) REFERENCES [ComputerDisks] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ComputerThresholdHistories_ComputerId_ActiveFrom] ON [ComputerThresholdHistories] ([ComputerId], [ActiveFrom]);

CREATE INDEX [IX_DiskThresholdHistories_ComputerDiskId_ActiveFrom] ON [DiskThresholdHistories] ([ComputerDiskId], [ActiveFrom]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260414131120_AddThresholdHistories', N'9.0.13');

DROP INDEX [IX_DiskMetrics_ComputerDiskId_CreatedAt] ON [DiskMetrics];

DROP INDEX [IX_ComputerMetrics_ComputerId_CreatedAt] ON [ComputerMetrics];

CREATE INDEX [IX_DiskMetrics_ComputerDiskId_CreatedAt] ON [DiskMetrics] ([ComputerDiskId], [CreatedAt]) INCLUDE ([UsedPercent]);

CREATE INDEX [IX_ComputerMetrics_ComputerId_CreatedAt] ON [ComputerMetrics] ([ComputerId], [CreatedAt]) INCLUDE ([CpuUsage], [RamUsage]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260414143455_AddCoveringIndexesForMetrics', N'9.0.13');

CREATE TABLE [MetricWarningLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [ComputerId] int NOT NULL,
    [MetricType] nvarchar(50) NOT NULL,
    [DiskName] nvarchar(200) NOT NULL,
    [MetricValue] float NOT NULL,
    [ThresholdValue] float NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MetricWarningLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MetricWarningLogs_Computers_ComputerId] FOREIGN KEY ([ComputerId]) REFERENCES [Computers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_MetricWarningLogs_ComputerId_MetricType_CreatedAt] ON [MetricWarningLogs] ([ComputerId], [MetricType], [CreatedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415082142_AddMetricWarningLogs', N'9.0.13');

DROP INDEX [IX_MetricWarningLogs_ComputerId_MetricType_CreatedAt] ON [MetricWarningLogs];

DECLARE @var59 sysname;
SELECT @var59 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MetricWarningLogs]') AND [c].[name] = N'MetricType');
IF @var59 IS NOT NULL EXEC(N'ALTER TABLE [MetricWarningLogs] DROP CONSTRAINT [' + @var59 + '];');
ALTER TABLE [MetricWarningLogs] DROP COLUMN [MetricType];

ALTER TABLE [MetricWarningLogs] ADD [MetricTypeId] int NOT NULL DEFAULT 0;

CREATE TABLE [MetricTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_MetricTypes] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[MetricTypes]'))
    SET IDENTITY_INSERT [MetricTypes] ON;
INSERT INTO [MetricTypes] ([Id], [Name])
VALUES (1, N'CPU'),
(2, N'RAM'),
(3, N'Disk');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[MetricTypes]'))
    SET IDENTITY_INSERT [MetricTypes] OFF;

CREATE INDEX [IX_MetricWarningLogs_ComputerId_MetricTypeId_CreatedAt] ON [MetricWarningLogs] ([ComputerId], [MetricTypeId], [CreatedAt]);

CREATE INDEX [IX_MetricWarningLogs_MetricTypeId] ON [MetricWarningLogs] ([MetricTypeId]);

ALTER TABLE [MetricWarningLogs] ADD CONSTRAINT [FK_MetricWarningLogs_MetricTypes_MetricTypeId] FOREIGN KEY ([MetricTypeId]) REFERENCES [MetricTypes] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415180418_AddMetricTypeLookup', N'9.0.13');

DECLARE @var60 sysname;
SELECT @var60 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MetricWarningLogs]') AND [c].[name] = N'DiskName');
IF @var60 IS NOT NULL EXEC(N'ALTER TABLE [MetricWarningLogs] DROP CONSTRAINT [' + @var60 + '];');
ALTER TABLE [MetricWarningLogs] DROP COLUMN [DiskName];

ALTER TABLE [MetricWarningLogs] ADD [ComputerDiskId] int NULL;

CREATE INDEX [IX_MetricWarningLogs_ComputerDiskId] ON [MetricWarningLogs] ([ComputerDiskId]);

ALTER TABLE [MetricWarningLogs] ADD CONSTRAINT [FK_MetricWarningLogs_ComputerDisks_ComputerDiskId] FOREIGN KEY ([ComputerDiskId]) REFERENCES [ComputerDisks] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415181631_ChangeDiskNameToForeignKey', N'9.0.13');

DROP TABLE [ComputerThresholdHistories];

DROP TABLE [DiskThresholdHistories];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415184850_DropThresholdHistoryTables', N'9.0.13');

ALTER TABLE [SidebarItems] ADD [ParentId] int NULL;

CREATE INDEX [IX_SidebarItems_ParentId] ON [SidebarItems] ([ParentId]);

ALTER TABLE [SidebarItems] ADD CONSTRAINT [FK_SidebarItems_SidebarItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [SidebarItems] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260512072806_AddSidebarHierarchy', N'9.0.13');

ALTER TABLE [Permissions] ADD [OrderIndex] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260514113932_AddPermissionOrderIndex', N'9.0.13');

ALTER TABLE [UserRoles] DROP CONSTRAINT [FK_UserRoles_Roles_RoleId1];

DROP INDEX [IX_UserRoles_RoleId1] ON [UserRoles];

DECLARE @var61 sysname;
SELECT @var61 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserRoles]') AND [c].[name] = N'RoleId1');
IF @var61 IS NOT NULL EXEC(N'ALTER TABLE [UserRoles] DROP CONSTRAINT [' + @var61 + '];');
ALTER TABLE [UserRoles] DROP COLUMN [RoleId1];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515071744_FinalFixEfWarnings', N'9.0.13');

DECLARE @var62 sysname;
SELECT @var62 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'Description');
IF @var62 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var62 + '];');
ALTER TABLE [Roles] DROP COLUMN [Description];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515123933_RemoveRoleDescription', N'9.0.13');

COMMIT;
GO

