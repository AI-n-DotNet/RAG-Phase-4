using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Benchmarks;

public static class BenchmarkDatasetPreparer
{
    public static async Task<BenchmarkDataset> PrepareAsync(string directory, IReadOnlyList<string> passages, string embeddingModel, IEmbeddingGenerator<string, Embedding<float>> generator, CancellationToken cancellationToken = default)
    {
        const string corpusVersion = "enterprise-synthetic-v1";
        const int dimensions = 1536;

        Directory.CreateDirectory(directory);

        var chunks = new List<BenchmarkDatasetChunk>(passages.Count);

        for (int index = 0; index < passages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int chunkId = index + 1;
            string path = Path.Combine(directory, $"chunk-{chunkId:D6}.json");

            BenchmarkDataset cached;

            if (File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path, cancellationToken);

                cached = JsonSerializer.Deserialize<BenchmarkDataset>(json) ?? throw new InvalidOperationException($"Invalid cache file: {path}");

                Console.WriteLine($"Reusing chunk {chunkId}.");
            }
            else
            {
                var embedding = await generator.GenerateAsync(passages[index], cancellationToken: cancellationToken);

                float[] vector = embedding.Vector.ToArray();

                if (vector.Length != dimensions || vector.Any(value => !float.IsFinite(value)) || !vector.Any(value => value != 0))
                {
                    throw new InvalidOperationException($"Chunk {chunkId} has an invalid embedding.");
                }

                cached = new BenchmarkDataset(
                    SchemaVersion: 1,
                    CorpusVersion: corpusVersion,
                    EmbeddingModel: embeddingModel,
                    Dimensions: dimensions,
                    Chunks:
                    [
                        new BenchmarkDatasetChunk(chunkId, passages[index], vector)
                    ]);

                // Publish the cache file only after writing finishes.
                string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";

                try
                {
                    await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(cached), cancellationToken);

                    File.Move(temporaryPath, path);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }

                Console.WriteLine($"Generated chunk {chunkId}.");
            }

            // Never silently reuse data from another model or corpus.
            if (cached.SchemaVersion != 1 || cached.CorpusVersion != corpusVersion || cached.EmbeddingModel != embeddingModel || cached.Dimensions != dimensions || cached.Chunks is null || cached.Chunks.Count != 1)
            {
                throw new InvalidOperationException($"Cache metadata mismatch: {path}");
            }

            var chunk = cached.Chunks[0];

            if (chunk is null || chunk.ChunkId != chunkId || chunk.Content != passages[index] || chunk.Embedding is null || chunk.Embedding.Length != dimensions || chunk.Embedding.Any(value => !float.IsFinite(value)) || !chunk.Embedding.Any(value => value != 0))
            {
                throw new InvalidOperationException($"Cache content mismatch: {path}");
            }

            chunks.Add(chunk);
        }

        return new BenchmarkDataset(1, corpusVersion, embeddingModel, dimensions, chunks);
    }
}