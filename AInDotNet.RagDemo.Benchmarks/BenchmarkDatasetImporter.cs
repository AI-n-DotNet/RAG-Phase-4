using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AInDotNet.RagDemo.BenchmarkData.Models;

namespace AInDotNet.RagDemo.Benchmarks;


public static class BenchmarkDatasetImporter
{
    public static async Task ImportAsync(AInDotNetRAG_BenchmarkContext db, BenchmarkDataset dataset, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(db.Database.GetDbConnection().Database, "AInDotNetRAG_Benchmark", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Import requires AInDotNetRAG_Benchmark.");
        }

        if (dataset.Chunks.Count == 0 || dataset.Chunks.Select(chunk => chunk.ChunkId).Distinct().Count() != dataset.Chunks.Count)
        {
            throw new InvalidOperationException("The dataset must contain chunks with unique IDs.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.Documents.AnyAsync(cancellationToken) || await db.DocumentChunks.AnyAsync(cancellationToken) || await db.LegacyBenchmarkChunks.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Benchmark tables must be empty. Nothing was imported.");
        }

        var document = new Document
        {
            Title = dataset.CorpusVersion,
            SourceUri = $"benchmark://{dataset.CorpusVersion}"
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        // Explicit IDs keep the saved dataset and database rows aligned.
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.DocumentChunks ON", cancellationToken);

        try
        {
            foreach (var chunk in dataset.Chunks)
            {
                db.DocumentChunks.Add(new DocumentChunk
                {
                    Id = chunk.ChunkId,
                    DocumentId = document.Id,
                    ChunkIndex = chunk.ChunkId - 1,
                    Content = chunk.Content,
                    Embedding = new SqlVector<float>(chunk.Embedding)
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            // Reset the session setting even when the operation is canceled.
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.DocumentChunks OFF", CancellationToken.None);
        }

        foreach (var chunk in dataset.Chunks)
        {
            db.LegacyBenchmarkChunks.Add(new LegacyBenchmarkChunk
            {
                Id = chunk.ChunkId,
                Content = chunk.Content,
                EmbeddingJson = JsonSerializer.Serialize(chunk.Embedding)
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Console.WriteLine($"Imported {dataset.Chunks.Count} native chunks " + $"and {dataset.Chunks.Count} matching JSON rows.");
    }
}