USE [master]
GO
/****** Nesnesi: Database [Staj2MonitoringDb] Betik Tarihi: 18.05.2026 11:46:43 ******/
CREATE DATABASE [Staj2MonitoringDb]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'Staj2MonitoringDb', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\DATA\Staj2MonitoringDb.mdf' , SIZE = 7479296KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'Staj2MonitoringDb_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\DATA\Staj2MonitoringDb_log.ldf' , SIZE = 9445376KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [Staj2MonitoringDb] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Staj2MonitoringDb].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Staj2MonitoringDb] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET ARITHABORT OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET AUTO_CLOSE ON 
GO
ALTER DATABASE [Staj2MonitoringDb] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Staj2MonitoringDb] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Staj2MonitoringDb] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET  ENABLE_BROKER 
GO
ALTER DATABASE [Staj2MonitoringDb] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Staj2MonitoringDb] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [Staj2MonitoringDb] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [Staj2MonitoringDb] SET  MULTI_USER 
GO
ALTER DATABASE [Staj2MonitoringDb] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Staj2MonitoringDb] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Staj2MonitoringDb] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Staj2MonitoringDb] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [Staj2MonitoringDb] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Staj2MonitoringDb] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [Staj2MonitoringDb] SET QUERY_STORE = ON
GO
ALTER DATABASE [Staj2MonitoringDb] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [Staj2MonitoringDb]
GO
/****** Nesnesi: User [mysz] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE USER [mysz] FOR LOGIN [mysz] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [mysz]
GO
/****** Nesnesi: PartitionFunction [PF_MetricDate] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE PARTITION FUNCTION [PF_MetricDate](datetime2(7)) AS RANGE RIGHT FOR VALUES (N'2026-02-01T00:00:00.000', N'2026-03-01T00:00:00.000', N'2026-04-01T00:00:00.000', N'2026-05-01T00:00:00.000', N'2026-06-01T00:00:00.000', N'2026-07-01T00:00:00.000')
GO
/****** Nesnesi: PartitionScheme [PS_MetricDate] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE PARTITION SCHEME [PS_MetricDate] AS PARTITION [PF_MetricDate] TO ([PRIMARY], [PRIMARY], [PRIMARY], [PRIMARY], [PRIMARY], [PRIMARY], [PRIMARY], [PRIMARY])
GO
/****** Nesnesi: Table [dbo].[__EFMigrationsHistory] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[ComputerDisks] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ComputerDisks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ComputerId] [int] NOT NULL,
	[DiskName] [nvarchar](200) NOT NULL,
	[TotalSizeGb] [float] NOT NULL,
	[ThresholdPercent] [float] NULL,
	[LastNotifyTime] [datetime2](7) NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[UpdatedBy] [int] NULL,
 CONSTRAINT [PK_ComputerDisks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[ComputerMetrics] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ComputerMetrics](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ComputerId] [int] NOT NULL,
	[CpuUsage] [float] NOT NULL,
	[RamUsage] [float] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL
) ON [PS_MetricDate]([CreatedAt])
GO
/****** Nesnesi: Table [dbo].[Computers] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Computers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MacAddress] [nvarchar](50) NOT NULL,
	[MachineName] [nvarchar](200) NOT NULL,
	[DisplayName] [nvarchar](200) NULL,
	[IpAddress] [nvarchar](200) NULL,
	[CpuModel] [nvarchar](200) NULL,
	[TotalRamMb] [float] NOT NULL,
	[LastSeen] [datetime2](7) NOT NULL,
	[RamLastNotifyTime] [datetime2](7) NULL,
	[CpuThreshold] [float] NULL,
	[RamThreshold] [float] NULL,
	[CpuLastNotifyTime] [datetime2](7) NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[UpdatedBy] [int] NULL,
	[IsOfflineAlertSent] [bit] NOT NULL,
 CONSTRAINT [PK_Computers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[ComputerTags] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ComputerTags](
	[ComputerId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedBy] [int] NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_ComputerTags] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[DiskMetrics] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DiskMetrics](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ComputerDiskId] [int] NOT NULL,
	[UsedPercent] [float] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DiskMetrics] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[MetricTypes] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MetricTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_MetricTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[MetricWarningLogs] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MetricWarningLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ComputerId] [int] NOT NULL,
	[MetricValue] [float] NOT NULL,
	[ThresholdValue] [float] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[MetricTypeId] [int] NOT NULL,
	[ComputerDiskId] [int] NULL,
 CONSTRAINT [PK_MetricWarningLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[PasswordSetupTokens] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PasswordSetupTokens](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RegistrationRequestId] [int] NOT NULL,
	[TokenHash] [nvarchar](200) NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
	[IsUsed] [bit] NOT NULL,
	[UsedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_PasswordSetupTokens] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[Permissions] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Permissions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[SidebarItemId] [int] NULL,
	[Description] [nvarchar](200) NOT NULL,
	[OrderIndex] [int] NOT NULL,
 CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[RefreshTokens] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RefreshTokens](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Token] [nvarchar](max) NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
	[IsRevoked] [bit] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[RegistrationRequests] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RegistrationRequests](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](450) NOT NULL,
	[Email] [nvarchar](450) NOT NULL,
	[RequestedRoleId] [int] NULL,
	[ApprovedByUserId] [int] NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[RejectedAt] [datetime2](7) NULL,
	[RejectionReason] [nvarchar](200) NULL,
	[Status] [int] NOT NULL,
	[RejectedBy] [int] NULL,
 CONSTRAINT [PK_RegistrationRequests] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[RolePermissions] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolePermissions](
	[RoleId] [int] NOT NULL,
	[PermissionId] [int] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC,
	[PermissionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[Roles] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedBy] [int] NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[UpdatedBy] [int] NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[SidebarItems] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SidebarItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](max) NOT NULL,
	[Icon] [nvarchar](max) NULL,
	[TargetView] [nvarchar](max) NOT NULL,
	[OrderIndex] [int] NOT NULL,
	[ParentId] [int] NULL,
 CONSTRAINT [PK_SidebarItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[Tags] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tags](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedBy] [int] NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
 CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[UserComputerAccesses] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserComputerAccesses](
	[UserId] [int] NOT NULL,
	[ComputerId] [int] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_UserComputerAccesses] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[ComputerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[UserRoles] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedBy] [int] NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[Users] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](450) NOT NULL,
	[Email] [nvarchar](200) NOT NULL,
	[PasswordHash] [nvarchar](200) NOT NULL,
	[IsApproved] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Table [dbo].[UserTagAccesses] Betik Tarihi: 18.05.2026 11:46:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserTagAccesses](
	[UserId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[DeletedBy] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_UserTagAccesses] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_ComputerDisks_ComputerId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerDisks_ComputerId] ON [dbo].[ComputerDisks]
(
	[ComputerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_ComputerMetrics_FastLookup] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerMetrics_FastLookup] ON [dbo].[ComputerMetrics]
(
	[ComputerId] ASC,
	[CreatedAt] ASC
)
INCLUDE([CpuUsage],[RamUsage]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PS_MetricDate]([CreatedAt])
GO
/****** Nesnesi: Index [IX_ComputerMetrics_Summary_Covering] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerMetrics_Summary_Covering] ON [dbo].[ComputerMetrics]
(
	[ComputerId] ASC
)
INCLUDE([CpuUsage],[RamUsage]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PS_MetricDate]([CreatedAt])
GO
/****** Nesnesi: Index [IX_ComputerMetrics_Trend_Covering] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerMetrics_Trend_Covering] ON [dbo].[ComputerMetrics]
(
	[ComputerId] ASC,
	[CreatedAt] ASC
)
INCLUDE([CpuUsage],[RamUsage]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PS_MetricDate]([CreatedAt])
GO
/****** Nesnesi: Index [IX_ComputerMetrics_Trend_Range] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerMetrics_Trend_Range] ON [dbo].[ComputerMetrics]
(
	[ComputerId] ASC,
	[CreatedAt] DESC
)
INCLUDE([CpuUsage],[RamUsage]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PS_MetricDate]([CreatedAt])
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_Computers_MacAddress] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Computers_MacAddress] ON [dbo].[Computers]
(
	[MacAddress] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_ComputerTags_ComputerId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerTags_ComputerId] ON [dbo].[ComputerTags]
(
	[ComputerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_ComputerTags_TagId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_ComputerTags_TagId] ON [dbo].[ComputerTags]
(
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_DiskMetrics_Trend_Range] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_DiskMetrics_Trend_Range] ON [dbo].[DiskMetrics]
(
	[ComputerDiskId] ASC,
	[CreatedAt] DESC
)
INCLUDE([UsedPercent]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_MetricWarningLogs_ComputerDiskId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_MetricWarningLogs_ComputerDiskId] ON [dbo].[MetricWarningLogs]
(
	[ComputerDiskId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_MetricWarningLogs_ComputerId_MetricTypeId_CreatedAt] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_MetricWarningLogs_ComputerId_MetricTypeId_CreatedAt] ON [dbo].[MetricWarningLogs]
(
	[ComputerId] ASC,
	[MetricTypeId] ASC,
	[CreatedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_MetricWarningLogs_MetricTypeId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_MetricWarningLogs_MetricTypeId] ON [dbo].[MetricWarningLogs]
(
	[MetricTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_MetricWarningLogs_Performance] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_MetricWarningLogs_Performance] ON [dbo].[MetricWarningLogs]
(
	[ComputerId] ASC,
	[CreatedAt] ASC
)
INCLUDE([MetricTypeId],[ComputerDiskId],[MetricValue],[ThresholdValue]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_PasswordSetupTokens_RegistrationRequestId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_PasswordSetupTokens_RegistrationRequestId] ON [dbo].[PasswordSetupTokens]
(
	[RegistrationRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_Permissions_Name] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Permissions_Name] ON [dbo].[Permissions]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_Permissions_SidebarItemId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_Permissions_SidebarItemId] ON [dbo].[Permissions]
(
	[SidebarItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_RefreshTokens_UserId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_RegistrationRequests_Email] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_RegistrationRequests_Email] ON [dbo].[RegistrationRequests]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_RegistrationRequests_Username] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_RegistrationRequests_Username] ON [dbo].[RegistrationRequests]
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_RolePermissions_PermissionId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions]
(
	[PermissionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_Roles_Name] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_Name] ON [dbo].[Roles]
(
	[Name] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_SidebarItems_ParentId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_SidebarItems_ParentId] ON [dbo].[SidebarItems]
(
	[ParentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_Tags_Name] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tags_Name] ON [dbo].[Tags]
(
	[Name] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_UserComputerAccesses_ComputerId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_UserComputerAccesses_ComputerId] ON [dbo].[UserComputerAccesses]
(
	[ComputerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_UserRoles_RoleId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_UserRoles_UserId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_UserRoles_UserId] ON [dbo].[UserRoles]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_Users_Email] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]
(
	[Email] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Nesnesi: Index [IX_Users_Username] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]
(
	[Username] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [IX_UserTagAccesses_TagId] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED INDEX [IX_UserTagAccesses_TagId] ON [dbo].[UserTagAccesses]
(
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Nesnesi: Index [NCCI_DiskMetrics_Analytics] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_DiskMetrics_Analytics] ON [dbo].[DiskMetrics]
(
	[ComputerDiskId],
	[UsedPercent]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
GO
/****** Nesnesi: Index [NCCI_MetricWarningLogs_Analytics] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_MetricWarningLogs_Analytics] ON [dbo].[MetricWarningLogs]
(
	[ComputerId],
	[MetricTypeId],
	[ComputerDiskId],
	[CreatedAt]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
GO
/****** Nesnesi: Index [CCI_ComputerMetrics] Betik Tarihi: 18.05.2026 11:46:44 ******/
CREATE CLUSTERED COLUMNSTORE INDEX [CCI_ComputerMetrics] ON [dbo].[ComputerMetrics] WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PS_MetricDate]([CreatedAt])
GO
ALTER TABLE [dbo].[Computers] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Computers] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Computers] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsOfflineAlertSent]
GO
ALTER TABLE [dbo].[ComputerTags] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ComputerTags] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[MetricWarningLogs] ADD  DEFAULT ((0)) FOR [MetricTypeId]
GO
ALTER TABLE [dbo].[Permissions] ADD  DEFAULT (N'') FOR [Description]
GO
ALTER TABLE [dbo].[Permissions] ADD  DEFAULT ((0)) FOR [OrderIndex]
GO
ALTER TABLE [dbo].[RegistrationRequests] ADD  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[RolePermissions] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Roles] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Tags] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Tags] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt]
GO
ALTER TABLE [dbo].[UserComputerAccesses] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[UserRoles] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt]
GO
ALTER TABLE [dbo].[UserRoles] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[UserTagAccesses] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[ComputerDisks]  WITH CHECK ADD  CONSTRAINT [FK_ComputerDisks_Computers_ComputerId] FOREIGN KEY([ComputerId])
REFERENCES [dbo].[Computers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ComputerDisks] CHECK CONSTRAINT [FK_ComputerDisks_Computers_ComputerId]
GO
ALTER TABLE [dbo].[ComputerMetrics]  WITH CHECK ADD  CONSTRAINT [FK_ComputerMetrics_Computers_ComputerId] FOREIGN KEY([ComputerId])
REFERENCES [dbo].[Computers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ComputerMetrics] CHECK CONSTRAINT [FK_ComputerMetrics_Computers_ComputerId]
GO
ALTER TABLE [dbo].[ComputerTags]  WITH CHECK ADD  CONSTRAINT [FK_ComputerTags_Computers_ComputerId] FOREIGN KEY([ComputerId])
REFERENCES [dbo].[Computers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ComputerTags] CHECK CONSTRAINT [FK_ComputerTags_Computers_ComputerId]
GO
ALTER TABLE [dbo].[ComputerTags]  WITH CHECK ADD  CONSTRAINT [FK_ComputerTags_Tags_TagId] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ComputerTags] CHECK CONSTRAINT [FK_ComputerTags_Tags_TagId]
GO
ALTER TABLE [dbo].[DiskMetrics]  WITH CHECK ADD  CONSTRAINT [FK_DiskMetrics_ComputerDisks_ComputerDiskId] FOREIGN KEY([ComputerDiskId])
REFERENCES [dbo].[ComputerDisks] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DiskMetrics] CHECK CONSTRAINT [FK_DiskMetrics_ComputerDisks_ComputerDiskId]
GO
ALTER TABLE [dbo].[MetricWarningLogs]  WITH CHECK ADD  CONSTRAINT [FK_MetricWarningLogs_ComputerDisks_ComputerDiskId] FOREIGN KEY([ComputerDiskId])
REFERENCES [dbo].[ComputerDisks] ([Id])
GO
ALTER TABLE [dbo].[MetricWarningLogs] CHECK CONSTRAINT [FK_MetricWarningLogs_ComputerDisks_ComputerDiskId]
GO
ALTER TABLE [dbo].[MetricWarningLogs]  WITH CHECK ADD  CONSTRAINT [FK_MetricWarningLogs_Computers_ComputerId] FOREIGN KEY([ComputerId])
REFERENCES [dbo].[Computers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MetricWarningLogs] CHECK CONSTRAINT [FK_MetricWarningLogs_Computers_ComputerId]
GO
ALTER TABLE [dbo].[MetricWarningLogs]  WITH CHECK ADD  CONSTRAINT [FK_MetricWarningLogs_MetricTypes_MetricTypeId] FOREIGN KEY([MetricTypeId])
REFERENCES [dbo].[MetricTypes] ([Id])
GO
ALTER TABLE [dbo].[MetricWarningLogs] CHECK CONSTRAINT [FK_MetricWarningLogs_MetricTypes_MetricTypeId]
GO
ALTER TABLE [dbo].[PasswordSetupTokens]  WITH CHECK ADD  CONSTRAINT [FK_PasswordSetupTokens_RegistrationRequests_RegistrationRequestId] FOREIGN KEY([RegistrationRequestId])
REFERENCES [dbo].[RegistrationRequests] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PasswordSetupTokens] CHECK CONSTRAINT [FK_PasswordSetupTokens_RegistrationRequests_RegistrationRequestId]
GO
ALTER TABLE [dbo].[Permissions]  WITH CHECK ADD  CONSTRAINT [FK_Permissions_SidebarItems_SidebarItemId] FOREIGN KEY([SidebarItemId])
REFERENCES [dbo].[SidebarItems] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Permissions] CHECK CONSTRAINT [FK_Permissions_SidebarItems_SidebarItemId]
GO
ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [FK_RefreshTokens_Users_UserId]
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY([PermissionId])
REFERENCES [dbo].[Permissions] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_Permissions_PermissionId]
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_Roles_RoleId]
GO
ALTER TABLE [dbo].[SidebarItems]  WITH CHECK ADD  CONSTRAINT [FK_SidebarItems_SidebarItems_ParentId] FOREIGN KEY([ParentId])
REFERENCES [dbo].[SidebarItems] ([Id])
GO
ALTER TABLE [dbo].[SidebarItems] CHECK CONSTRAINT [FK_SidebarItems_SidebarItems_ParentId]
GO
ALTER TABLE [dbo].[UserComputerAccesses]  WITH CHECK ADD  CONSTRAINT [FK_UserComputerAccesses_Computers_ComputerId] FOREIGN KEY([ComputerId])
REFERENCES [dbo].[Computers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserComputerAccesses] CHECK CONSTRAINT [FK_UserComputerAccesses_Computers_ComputerId]
GO
ALTER TABLE [dbo].[UserComputerAccesses]  WITH CHECK ADD  CONSTRAINT [FK_UserComputerAccesses_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserComputerAccesses] CHECK CONSTRAINT [FK_UserComputerAccesses_Users_UserId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles_RoleId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users_UserId]
GO
ALTER TABLE [dbo].[UserTagAccesses]  WITH CHECK ADD  CONSTRAINT [FK_UserTagAccesses_Tags_TagId] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserTagAccesses] CHECK CONSTRAINT [FK_UserTagAccesses_Tags_TagId]
GO
ALTER TABLE [dbo].[UserTagAccesses]  WITH CHECK ADD  CONSTRAINT [FK_UserTagAccesses_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserTagAccesses] CHECK CONSTRAINT [FK_UserTagAccesses_Users_UserId]
GO
USE [master]
GO
ALTER DATABASE [Staj2MonitoringDb] SET  READ_WRITE 
GO
