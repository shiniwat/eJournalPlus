USE [meetF_user_205]
GO
/****** Object:  Table [dbo].[Assignments]    ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Assignments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](50) NOT NULL,
	[Description] [varchar](500) NOT NULL,
	[StudyCount] [int] NOT NULL,
	[OwnerId] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
	[Version] [int] NOT NULL,
	[IsAvailable] [bit] NOT NULL,
	[DataSize] [bigint] NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[CommentCount] [int] NOT NULL,
	[CourseId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[StudyMetaData]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[StudyMetaData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](50) NOT NULL,
	[Description] [varchar](500) NOT NULL,
	[OwnerId] [int] NOT NULL,
	[ParentAssignmentId] [int] NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
	[IsAvailable] [bit] NOT NULL,
	[CommentCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  ForeignKey [FK__StudyMeta__Paren__0519C6AF]    Script Date: 03/13/2009 12:38:53 ******/
ALTER TABLE [dbo].[StudyMetaData]  WITH NOCHECK ADD FOREIGN KEY([ParentAssignmentId])
REFERENCES [dbo].[Assignments] ([Id])
GO

/*---- */
USE [meetF_user_206]
GO
/****** Object:  Table [dbo].[Assignments]    ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Assignments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](50) NOT NULL,
	[Description] [varchar](500) NOT NULL,
	[StudyCount] [int] NOT NULL,
	[OwnerId] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
	[Version] [int] NOT NULL,
	[IsAvailable] [bit] NOT NULL,
	[DataSize] [bigint] NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[CommentCount] [int] NOT NULL,
	[CourseId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[StudyMetaData]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[StudyMetaData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](50) NOT NULL,
	[Description] [varchar](500) NOT NULL,
	[OwnerId] [int] NOT NULL,
	[ParentAssignmentId] [int] NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
	[IsAvailable] [bit] NOT NULL,
	[CommentCount] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  ForeignKey [FK__StudyMeta__Paren__0519C6AF]    Script Date: 03/13/2009 12:38:53 ******/
ALTER TABLE [dbo].[StudyMetaData]  WITH NOCHECK ADD FOREIGN KEY([ParentAssignmentId])
REFERENCES [dbo].[Assignments] ([Id])
GO
