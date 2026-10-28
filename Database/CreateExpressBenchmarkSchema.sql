/*
SQL Server 2025 Express benchmark database.
Creates empty tables; imports will reuse existing cached embeddings.
Run once on the Express instance.
*/

USE [master];
GO

CREATE DATABASE [AInDotNetRAG_Benchmark];
GO

ALTER DATABASE [AInDotNetRAG_Benchmark]
SET COMPATIBILITY_LEVEL = 170;
GO

USE [AInDotNetRAG_Benchmark];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Documents
(
    Id int IDENTITY(1,1) NOT NULL,
    Title nvarchar(500) NOT NULL,
    SourceUri nvarchar(2000) NULL,
    CreatedUtc datetime2(7) NOT NULL
        CONSTRAINT DF_Documents_CreatedUtc
        DEFAULT (sysutcdatetime()),

    CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE TABLE dbo.DocumentChunks
(
    Id int IDENTITY(1,1) NOT NULL,
    DocumentId int NOT NULL,
    ChunkIndex int NOT NULL,
    Content nvarchar(max) NOT NULL,
    Embedding vector(1536, float32) NOT NULL,

    CONSTRAINT PK_DocumentChunks PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT UQ_DocumentChunks_DocumentIndex
        UNIQUE NONCLUSTERED (DocumentId, ChunkIndex),

    CONSTRAINT FK_DocumentChunks_Documents
        FOREIGN KEY (DocumentId) REFERENCES dbo.Documents(Id)
);
GO

CREATE NONCLUSTERED INDEX IX_DocumentChunks_DocumentId
ON dbo.DocumentChunks(DocumentId);
GO

CREATE TABLE dbo.LegacyBenchmarkChunks
(
    Id int NOT NULL,
    Content nvarchar(max) NOT NULL,
    EmbeddingJson nvarchar(max) NOT NULL,

    CONSTRAINT PK_LegacyBenchmarkChunks
        PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT CK_LegacyBenchmarkChunks_EmbeddingJson
        CHECK (ISJSON(EmbeddingJson, ARRAY) = 1)
);
GO

SELECT
    SERVERPROPERTY('ServerName') AS ServerName,
    SERVERPROPERTY('Edition') AS Edition,
    SERVERPROPERTY('ProductVersion') AS ProductVersion;

SELECT name
FROM sys.tables
ORDER BY name;