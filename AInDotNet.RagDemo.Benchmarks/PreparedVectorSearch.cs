using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

public static class PreparedVectorSearch
{
    public static IReadOnlyList<VectorMatch> Search(float[] query, PreparedVectorCache cache, int topK)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        if (query.Length != cache.Dimensions)
        {
            throw new ArgumentException("Query dimensions must match the cache.", nameof(query));
        }

        // Validate and calculate the question's magnitude once per search.
        double querySquaredMagnitude = 0;

        foreach (float value in query)
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Query values must be finite.", nameof(query));
            }

            double component = value;
            querySquaredMagnitude += component * component;
        }

        if (querySquaredMagnitude == 0)
        {
            throw new ArgumentException("The query must not be a zero vector.", nameof(query));
        }

        double queryMagnitude = Math.Sqrt(querySquaredMagnitude);
        var matches = new List<VectorMatch>(cache.Count);

        foreach (var entry in cache.Entries)
        {
            double dotProduct = 0;

            // Stored vectors were validated during cache preparation.
            for (int index = 0; index < query.Length; index++)
            {
                dotProduct += (double)query[index] * entry.Embedding[index];
            }

            double similarity = dotProduct / (queryMagnitude * entry.Magnitude);

            matches.Add(new VectorMatch(entry.ChunkId, similarity));
        }

        // Keep the original ranking approach to isolate the effect
        // of preparing magnitudes and moving validation.
        return matches.OrderByDescending(match => match.Similarity).ThenBy(match => match.ChunkId).Take(topK).ToArray();
    }
}