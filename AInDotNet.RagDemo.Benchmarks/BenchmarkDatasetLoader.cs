using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.Text.Json;
using AInDotNet.RagDemo.BenchmarkData.Models;

namespace AInDotNet.RagDemo.Benchmarks;


public static class BenchmarkDatasetLoader
{
    public static async Task LoadSmallDatasetAsync(AInDotNetRAG_BenchmarkContext db, IEmbeddingGenerator<string, Embedding<float>> generator, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(db.Database.GetDbConnection().Database, "AInDotNetRAG_Benchmark", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Dataset loading requires AInDotNetRAG_Benchmark.");
        }

        // Start with an empty benchmark database.
        // Refuse to overwrite or append to an existing dataset.
        if (await db.Documents.AnyAsync(cancellationToken) || await db.DocumentChunks.AnyAsync(cancellationToken) || await db.LegacyBenchmarkChunks.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("The benchmark tables must be empty. No data was changed.");
        }

        string[] passages =
        [
            "SQL Server stores document embeddings as native vectors. " + "Cosine distance identifies nearby document chunks.",
            "A gardening handbook recommends preparing soil before " + "planting vegetables and watering seedlings regularly.",
            "Project Cedar keeps its emergency recovery keys in the " + "Northbridge vault. Access requires approval."
        ];

        var document = new Document
        {
            Title = "Benchmark setup dataset",
            SourceUri = "benchmark://setup/v1"
        };

        // Finish API calls before beginning the database transaction.
        for (int index = 0; index < passages.Length; index++)
        {
            var embedding = await generator.GenerateAsync(passages[index], cancellationToken: cancellationToken);

            if (embedding.Vector.Length != 1536)
            {
                throw new InvalidOperationException($"Expected 1536 dimensions; " + $"received {embedding.Vector.Length}.");
            }

            document.DocumentChunks.Add(new DocumentChunk
            {
                ChunkIndex = index,
                Content = passages[index],
                Embedding = new SqlVector<float>(embedding.Vector)
            });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        // Native chunk IDs are now available.
        // Preserve those IDs and vector values in the JSON rows.
        foreach (var chunk in document.DocumentChunks)
        {
            db.LegacyBenchmarkChunks.Add(new LegacyBenchmarkChunk
            {
                Id = chunk.Id,
                Content = chunk.Content,
                EmbeddingJson = JsonSerializer.Serialize(chunk.Embedding.Memory.ToArray())
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Console.WriteLine($"Loaded {document.DocumentChunks.Count} native-vector chunks " + "and matching JSON rows.");
    }
}