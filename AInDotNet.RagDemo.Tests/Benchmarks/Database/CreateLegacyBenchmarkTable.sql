/*
Benchmark storage compatible with SQL Server 2022 and later.
Embeddings are stored as JSON; similarity is calculated in C#.
*/

USE [AInDotNetRAG_Benchmark];
GO

CREATE TABLE [dbo].[LegacyBenchmarkChunks]
(
    [Id] int NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [EmbeddingJson] nvarchar(max) NOT NULL,

    CONSTRAINT [PK_LegacyBenchmarkChunks]
        PRIMARY KEY ([Id]),

    CONSTRAINT [CK_LegacyBenchmarkChunks_EmbeddingJson]
        CHECK (ISJSON([EmbeddingJson], ARRAY) = 1)
);
GO