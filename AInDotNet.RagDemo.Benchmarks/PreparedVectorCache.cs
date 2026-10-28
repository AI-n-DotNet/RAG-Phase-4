using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

public sealed class PreparedVectorCache
{
    internal sealed record Entry(int ChunkId, float[] Embedding, double Magnitude);

    internal IReadOnlyList<Entry> Entries { get; }

    public int Count => Entries.Count;

    public int Dimensions { get; }

    public PreparedVectorCache(IReadOnlyList<BenchmarkVector> vectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);

        if (vectors.Count == 0)
        {
            throw new ArgumentException("The cache requires at least one vector.", nameof(vectors));
        }

        var entries = new List<Entry>(vectors.Count);
        var ids = new HashSet<int>();

        int dimensions = 0;

        foreach (var vector in vectors)
        {
            if (vector is null || vector.Embedding is null || vector.Embedding.Length == 0)
            {
                throw new ArgumentException("Every entry requires a nonempty vector.", nameof(vectors));
            }

            if (!ids.Add(vector.ChunkId))
            {
                throw new ArgumentException("Chunk IDs must be unique.", nameof(vectors));
            }

            if (dimensions == 0)
                dimensions = vector.Embedding.Length;

            if (vector.Embedding.Length != dimensions)
            {
                throw new ArgumentException("All vectors must have the same dimensions.", nameof(vectors));
            }

            // Own a copy so changes to the original array cannot
            // invalidate the stored magnitude.
            float[] embedding = (float[])vector.Embedding.Clone();

            double squaredMagnitude = 0;

            foreach (float value in embedding)
            {
                if (!float.IsFinite(value))
                {
                    throw new ArgumentException("Vector values must be finite.", nameof(vectors));
                }

                double component = value;
                squaredMagnitude += component * component;
            }

            if (squaredMagnitude == 0)
            {
                throw new ArgumentException("Zero vectors are not supported.", nameof(vectors));
            }

            entries.Add(new Entry(vector.ChunkId, embedding, Math.Sqrt(squaredMagnitude)));
        }

        Entries = entries.AsReadOnly();
        Dimensions = dimensions;
    }
}