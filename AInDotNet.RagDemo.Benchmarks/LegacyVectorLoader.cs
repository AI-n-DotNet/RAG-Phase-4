using AInDotNet.RagDemo.BenchmarkData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AInDotNet.RagDemo.Benchmarks;

public static class LegacyVectorLoader
{
    public static async Task<IReadOnlyList<BenchmarkVector>> LoadAsync(AInDotNetRAG_BenchmarkContext db, CancellationToken cancellationToken = default)
    {
        // Transfer vectors and IDs only. Fetch winning chunks' text later.
        var rows = await db.Database.SqlQueryRaw<LegacyVectorRow>("""SELECT Id, EmbeddingJson FROM dbo.LegacyBenchmarkChunks ORDER BY Id""").ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            float[] embedding = JsonSerializer.Deserialize<float[]>(row.EmbeddingJson) ?? throw new InvalidOperationException($"Chunk {row.Id} has a null embedding.");

            if (embedding.Length != 1536)
            {
                throw new InvalidOperationException($"Chunk {row.Id}: expected 1536 dimensions, " + $"received {embedding.Length}.");
            }

            return new BenchmarkVector(row.Id, embedding);
        }).ToArray();
    }
}

public sealed class LegacyVectorRow
{
    public int Id { get; set; }
    public string EmbeddingJson { get; set; } = string.Empty;
}