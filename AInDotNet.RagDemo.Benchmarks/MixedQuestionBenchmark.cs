using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using DeveloperModels = AInDotNet.RagDemo.BenchmarkData.Models;
using ExpressModels = AInDotNet.RagDemo.ExpressData.Models;

namespace AInDotNet.RagDemo.Benchmarks;


public static class MixedQuestionBenchmark
{
    public static async Task RunAsync(string developerConnection, string expressConnection, string directory, string embeddingModel, int workers = 16)
    {
        if (workers is not (4 or 8 or 16))
            throw new ArgumentOutOfRangeException(nameof(workers), "Use 4, 8, or 16 workers."); 
        
        const int topK = 5;

        // Read and validate every question before starting measurements.
        var vectors = new List<SqlVector<float>>();

        foreach (var question in BenchmarkQuestions.All)
        {
            string path = Path.Combine(directory, question.CacheName + ".json");

            var saved = JsonSerializer.Deserialize<CachedBenchmarkQuery>(await File.ReadAllTextAsync(path));

            if (saved is null || saved.SchemaVersion != 1 || saved.EmbeddingModel != embeddingModel || saved.Question != question.Text || saved.Embedding is null || saved.Embedding.Length != 1536 || saved.Embedding.Any(value => !float.IsFinite(value)) || !saved.Embedding.Any(value => value != 0))
            {
                throw new InvalidOperationException($"Invalid question cache: {path}");
            }

            vectors.Add(new SqlVector<float>(saved.Embedding));
        }

        var developerOptions = new DbContextOptionsBuilder<DeveloperModels.AInDotNetRAG_BenchmarkContext>().UseSqlServer(developerConnection).Options;

        var expressOptions = new DbContextOptionsBuilder<ExpressModels.AInDotNetRAG_ExpressContext>().UseSqlServer(expressConnection).Options;

        await using (var developer = new DeveloperModels.AInDotNetRAG_BenchmarkContext(developerOptions))
        await using (var express = new ExpressModels.AInDotNetRAG_ExpressContext(expressOptions))
        {
            int developerCount = await developer.DocumentChunks.CountAsync();
            int expressCount = await express.DocumentChunks.CountAsync();

            if (developerCount != 10000 || expressCount != 10000)
            {
                throw new InvalidOperationException("Both instances must contain 10,000 native chunks.");
            }
        }

        Console.WriteLine("Dataset: 10,000 chunks");
        Console.WriteLine($"Saved questions: {vectors.Count}");
        Console.WriteLine("Top K: 5");
        Console.WriteLine("Question selection: deterministic rotation");
        Console.WriteLine("Ranking only; no embedding requests.");

        await ConcurrentRetrievalTimer.MeasureQueriesAsync($"Express — mixed questions, {workers} workers", () => new ExpressModels.AInDotNetRAG_ExpressContext(expressOptions),
            async (workerDb, worker, index) =>
            {
                var vector = vectors[(worker + index) % vectors.Count];

                var matches = await workerDb.DocumentChunks
                    .AsNoTracking()
                    .Select(chunk => new
                    {
                        ChunkId = chunk.Id,
                        Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, vector)
                    })
                    .OrderBy(match => match.Distance)
                    .ThenBy(match => match.ChunkId)
                    .Take(topK)
                    .ToListAsync();

                return matches.Count;
            },
            expectedResults: topK,
            workers: workers,
            searchesPerWorker: 1000);

        await ConcurrentRetrievalTimer.MeasureQueriesAsync($"Developer — mixed questions, {workers} workers", () => new DeveloperModels.AInDotNetRAG_BenchmarkContext(developerOptions),
            async (workerDb, worker, index) =>
            {
                var vector = vectors[(worker + index) % vectors.Count];

                var matches = await workerDb.DocumentChunks
                    .AsNoTracking()
                    .Select(chunk => new
                    {
                        ChunkId = chunk.Id,
                        Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, vector)
                    })
                    .OrderBy(match => match.Distance)
                    .ThenBy(match => match.ChunkId)
                    .Take(topK)
                    .ToListAsync();

                return matches.Count;
            },
            expectedResults: topK,
            workers: workers,
            searchesPerWorker: 1000);

    }
}