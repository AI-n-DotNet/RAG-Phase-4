using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using DeveloperModels = AInDotNet.RagDemo.BenchmarkData.Models;
using ExpressModels = AInDotNet.RagDemo.ExpressData.Models;

namespace AInDotNet.RagDemo.Benchmarks;

public static class BenchmarkQuestionVerifier
{
    public static async Task VerifyAsync(DeveloperModels.AInDotNetRAG_BenchmarkContext developer, ExpressModels.AInDotNetRAG_ExpressContext express, string directory, string embeddingModel)
    {
        const int topK = 5;
        const double tolerance = 0.00001;

        foreach (var question in BenchmarkQuestions.All)
        {
            string path = Path.Combine(directory, question.CacheName + ".json");

            var saved = JsonSerializer.Deserialize<CachedBenchmarkQuery>(await File.ReadAllTextAsync(path));

            if (saved is null || saved.SchemaVersion != 1 || saved.EmbeddingModel != embeddingModel || saved.Question != question.Text || saved.Embedding is null || saved.Embedding.Length != 1536 || saved.Embedding.Any(value => !float.IsFinite(value)) || !saved.Embedding.Any(value => value != 0))
            {
                throw new InvalidOperationException($"Invalid question cache: {path}");
            }

            var vector = new SqlVector<float>(saved.Embedding);

            var developerMatches = await developer.DocumentChunks
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

            var expressMatches = await express.DocumentChunks
                .AsNoTracking()
                .Select(chunk => new
                {
                    ChunkId = chunk.Id,
                    Distance = EF.Functions.VectorDistance(
                        "cosine", chunk.Embedding, vector)
                })
                .OrderBy(match => match.Distance)
                .ThenBy(match => match.ChunkId)
                .Take(topK)
                .ToListAsync();

            if (developerMatches.Count != topK || expressMatches.Count != topK)
            {
                throw new InvalidOperationException($"{question.CacheName}: expected five results.");
            }

            for (int index = 0; index < topK; index++)
            {
                var left = developerMatches[index];
                var right = expressMatches[index];

                if (left.ChunkId != right.ChunkId || !double.IsFinite(left.Distance) || !double.IsFinite(right.Distance) || Math.Abs(left.Distance - right.Distance) > tolerance)
                {
                    throw new InvalidOperationException(
                        $"{question.CacheName}: results differ " +
                        $"at rank {index + 1}. " +
                        $"Developer ID={left.ChunkId}, " +
                        $"distance={left.Distance:R}; " +
                        $"Express ID={right.ChunkId}, " +
                        $"distance={right.Distance:R}.");
                }
            }

            Console.WriteLine($"PASS: {question.CacheName}; " + $"five IDs and scores agree.");
        }

        Console.WriteLine($"Verified {BenchmarkQuestions.All.Count} questions.");
    }
}