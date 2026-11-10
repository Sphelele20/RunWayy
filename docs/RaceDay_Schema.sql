-- RaceDay implementation-aligned schema. Run on a clean SQL Server instance.
-- Never overwrite an existing database containing coursework data.
IF DB_ID(N'RaceDayPlanningDb') IS NULL CREATE DATABASE [RaceDayPlanningDb];
GO
USE [RaceDayPlanningDb];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUsers') THROW 50001, 'Use a fresh empty RaceDayPlanningDb database; existing data has not been changed.', 1;
GO
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FirstName] nvarchar(50) NOT NULL,
    [LastName] nvarchar(50) NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [ProfileImagePath] nvarchar(260) NULL,
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
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Events] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Date] datetime2 NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [DistanceKm] decimal(8,2) NOT NULL,
    [EventType] nvarchar(20) NOT NULL,
    [BannerBlobName] nvarchar(260) NULL,
    [OrganiserId] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Events_AspNetUsers_OrganiserId] FOREIGN KEY ([OrganiserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [DistanceKm] decimal(8,2) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Enrolments] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [ParticipantId] nvarchar(450) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [EnrolledAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Enrolments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Enrolments_AspNetUsers_ParticipantId] FOREIGN KEY ([ParticipantId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Enrolments_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Enrolments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Results] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [EnrolmentId] int NOT NULL,
    [FinishTime] time NOT NULL,
    [Position] int NOT NULL,
    [PublishedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Results] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Results_Enrolments_EnrolmentId] FOREIGN KEY ([EnrolmentId]) REFERENCES [Enrolments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Results_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_Categories_EventId_Name] ON [Categories] ([EventId], [Name]);
GO


CREATE INDEX [IX_Enrolments_CategoryId] ON [Enrolments] ([CategoryId]);
GO


CREATE UNIQUE INDEX [IX_Enrolments_EventId_ParticipantId] ON [Enrolments] ([EventId], [ParticipantId]);
GO


CREATE INDEX [IX_Enrolments_ParticipantId] ON [Enrolments] ([ParticipantId]);
GO


CREATE INDEX [IX_Events_Date] ON [Events] ([Date]);
GO


CREATE INDEX [IX_Events_OrganiserId] ON [Events] ([OrganiserId]);
GO


CREATE UNIQUE INDEX [IX_Results_EnrolmentId] ON [Results] ([EnrolmentId]);
GO


CREATE UNIQUE INDEX [IX_Results_EventId_Position] ON [Results] ([EventId], [Position]);
GO



GO
-- Realistic sample users: two organisers and two participants. Passwords are Identity hashes.
INSERT [AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp]) VALUES (N'role-organiser',N'Organiser',N'ORGANISER',N'role-org-stamp'),(N'role-participant',N'Participant',N'PARTICIPANT',N'role-part-stamp'),(N'role-admin',N'Admin',N'ADMIN',N'role-admin-stamp');
INSERT [AspNetUsers] ([Id],[FirstName],[LastName],[Gender],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES (N'org-thabo',N'Thabo',N'Mbeki',N'PreferNotToSay',N'thabo@raceday.co.za',N'THABO@RACEDAY.CO.ZA',N'thabo@raceday.co.za',N'THABO@RACEDAY.CO.ZA',1,N'AQAAAAIAAYagAAAAEI8RGYqVyG9b4VCqUR75gz18GDPaOou9DxkNbgYkXsOAianLJvOyI9GwYvBgoQHO8Q==',N'99900cd9-4478-4d92-b4e9-2e320a1816b8',N'060f4b93-bf71-466f-8f2a-8eee157fb7a8',0,0,1,0);
INSERT [AspNetUserRoles] ([UserId],[RoleId]) VALUES(N'org-thabo',N'role-organiser');
INSERT [AspNetUsers] ([Id],[FirstName],[LastName],[Gender],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES (N'org-nadia',N'Nadia',N'Williams',N'PreferNotToSay',N'nadia@raceday.co.za',N'NADIA@RACEDAY.CO.ZA',N'nadia@raceday.co.za',N'NADIA@RACEDAY.CO.ZA',1,N'AQAAAAIAAYagAAAAEATWE6bwb2khUL8MawFZkjLLLr0RyflNi5ZAK8+RrPi0nCKT+FKonu+TPsUZGvdi3w==',N'6b1b4f23-2cf2-43ab-8078-78929a5f22c6',N'ccead849-488f-444f-bc3d-00a656afb2ab',0,0,1,0);
INSERT [AspNetUserRoles] ([UserId],[RoleId]) VALUES(N'org-nadia',N'role-organiser');
INSERT [AspNetUsers] ([Id],[FirstName],[LastName],[Gender],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES (N'part-sipho',N'Sipho',N'Ndlovu',N'PreferNotToSay',N'sipho@example.com',N'SIPHO@EXAMPLE.COM',N'sipho@example.com',N'SIPHO@EXAMPLE.COM',1,N'AQAAAAIAAYagAAAAEJtY0AttwjeLWHQVRrQzZeeQflX7rqxU3x4E5nGPcih/28ZrtAj//YOf5jlznvrQYg==',N'a4d8f8cd-7706-4f38-9077-037d78eeae71',N'8f254741-8fca-49a7-97d6-ca2bda406c0c',0,0,1,0);
INSERT [AspNetUserRoles] ([UserId],[RoleId]) VALUES(N'part-sipho',N'role-participant');
INSERT [AspNetUsers] ([Id],[FirstName],[LastName],[Gender],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES (N'part-lerato',N'Lerato',N'Dlamini',N'PreferNotToSay',N'lerato@example.com',N'LERATO@EXAMPLE.COM',N'lerato@example.com',N'LERATO@EXAMPLE.COM',1,N'AQAAAAIAAYagAAAAEF0xPA+kyP0Uliv5LKSleO8n2aXveB2wetTNF4Bizn9UQ2B7FuUou7RFUkzpA112xg==',N'69265310-90ae-4ebb-83dd-9d54dec7b608',N'394c4f78-e5cb-4b4b-bf63-5d7ff625b297',0,0,1,0);
INSERT [AspNetUserRoles] ([UserId],[RoleId]) VALUES(N'part-lerato',N'role-participant');

SET IDENTITY_INSERT [Events] ON;
INSERT [Events] ([Id],[Name],[Description],[Date],[Location],[DistanceKm],[EventType],[OrganiserId],[CreatedAt]) VALUES
(1,N'Soweto Community 10K',N'Community road running race',DATEADD(day,-7,DATEADD(hour,2,GETUTCDATE())),N'Soweto, Johannesburg',10,N'Run',N'org-thabo',GETUTCDATE()),
(2,N'Cape Town Charity Walk',N'A family charity walk',DATEADD(day,21,DATEADD(hour,2,GETUTCDATE())),N'Sea Point, Cape Town',5,N'Walk',N'org-nadia',GETUTCDATE()),
(3,N'Durban Coastal Cycle',N'A timed coastal cycle event',DATEADD(day,35,DATEADD(hour,2,GETUTCDATE())),N'Durban Beachfront',25,N'Cycle',N'org-thabo',GETUTCDATE());
SET IDENTITY_INSERT [Events] OFF;
SET IDENTITY_INSERT [Categories] ON;
INSERT [Categories] ([Id],[EventId],[Name],[DistanceKm]) VALUES(1,1,N'10 km Open',10),(2,1,N'5 km Social',5),(3,2,N'5 km Walk',5),(4,3,N'25 km Cycle',25);
SET IDENTITY_INSERT [Categories] OFF;
SET IDENTITY_INSERT [Enrolments] ON;
INSERT [Enrolments] ([Id],[EventId],[CategoryId],[ParticipantId],[Status],[EnrolledAt]) VALUES
(1,1,1,N'part-sipho',N'Confirmed',DATEADD(day,-14,GETUTCDATE())),
(2,1,1,N'part-lerato',N'Confirmed',DATEADD(day,-14,GETUTCDATE())),
(3,2,3,N'part-sipho',N'Pending',GETUTCDATE()),
(4,3,4,N'part-lerato',N'Confirmed',GETUTCDATE());
SET IDENTITY_INSERT [Enrolments] OFF;
SET IDENTITY_INSERT [Results] ON;
INSERT [Results] ([Id],[EventId],[EnrolmentId],[FinishTime],[Position],[PublishedAt]) VALUES
(1,1,1,'00:47:12',1,GETUTCDATE()),(2,1,2,'00:51:30',2,GETUTCDATE());
SET IDENTITY_INSERT [Results] OFF;
GO
IF (SELECT COUNT(*) FROM sys.tables) <> 11 THROW 50002, 'Unexpected schema table count.',1;
IF (SELECT COUNT(*) FROM [Events]) <> 3 THROW 50003, 'Sample events are missing.',1;
IF (SELECT COUNT(*) FROM [AspNetUsers]) <> 4 THROW 50004, 'Sample users are missing.',1;
PRINT 'RaceDay schema and sample data verified successfully.';
GO
