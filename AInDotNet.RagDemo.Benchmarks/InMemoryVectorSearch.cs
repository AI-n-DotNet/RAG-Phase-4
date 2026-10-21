using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;


public sealed record BenchmarkVector(int ChunkId, float[] Embedding);

public sealed record VectorMatch(int ChunkId, double Similarity);

public static class InMemoryVectorSearch
{
    public static IReadOnlyList<VectorMatch> Search(float[] query, IReadOnlyList<BenchmarkVector> vectors, int topK)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        // Higher cosine similarity means a closer match.
        // Chunk ID provides consistent ordering when scores tie.
        return vectors
            .Select(vector => new VectorMatch(vector.ChunkId, CosineSimilarity.Calculate(query, vector.Embedding)))
            .OrderByDescending(match => match.Similarity)
            .ThenBy(match => match.ChunkId)
            .Take(topK)
            .ToArray();
    }
}