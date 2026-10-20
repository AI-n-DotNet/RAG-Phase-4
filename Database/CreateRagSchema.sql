/*
NOTE:
This project requires SQL Server 2025 (17.x). This demonstration uses native vector functionality unavailable in SQL Server 2022 and earlier.
SQL Server 2025 Developer Edition is suitable for running this demonstration. It is free for development and testing and is not licensed for production use.
Updating SSMS alone does not add vector support.
Run this script once to create a new database. The target database must not already exist.
*/

USE [master]
GO
-- Use the SQL Server instance's default data and log directories.
CREATE DATABASE [AInDotNetRAG];
GO
ALTER DATABASE [AInDotNetRAG] SET COMPATIBILITY_LEVEL = 170
GO

USE [AInDotNetRAG]
GO

/****** Object:  Table [dbo].[DocumentChunks]    Script Date: 10/20/2026 8:08:52 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DocumentChunks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DocumentId] [int] NOT NULL,
	[ChunkIndex] [int] NOT NULL,
	[Content] [nvarchar](max) NOT NULL,
	[Embedding] [vector](1536, float32) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_DocumentChunks_DocumentIndex] UNIQUE NONCLUSTERED 
(
	[DocumentId] ASC,
	[ChunkIndex] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Documents]    Script Date: 10/20/2026 8:08:52 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Documents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](500) NOT NULL,
	[SourceUri] [nvarchar](2000) NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentChunks_DocumentId]    Script Date: 10/20/2026 8:08:52 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentChunks_DocumentId] ON [dbo].[DocumentChunks]
(
	[DocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Documents] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[DocumentChunks]  WITH CHECK ADD  CONSTRAINT [FK_DocumentChunks_Documents] FOREIGN KEY([DocumentId])
REFERENCES [dbo].[Documents] ([Id])
GO
ALTER TABLE [dbo].[DocumentChunks] CHECK CONSTRAINT [FK_DocumentChunks_Documents]
GO
USE [master]
GO
ALTER DATABASE [AInDotNetRAG] SET  READ_WRITE 
GO
