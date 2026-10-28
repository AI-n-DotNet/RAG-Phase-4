using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Benchmarks;


public static class BenchmarkDatasetReader
{
    public static async Task<BenchmarkDataset> LoadAsync(string directory, string embeddingModel, CancellationToken cancellationToken = default)
    {
        var passages = BenchmarkCorpus.Create();
        var chunks = new List<BenchmarkDatasetChunk>(passages.Count);

        for (int index = 0; index < passages.Count; index++)
        {
            int chunkId = index + 1;

            string path = Path.Combine(directory, $"chunk-{chunkId:D6}.json");

            string json = await File.ReadAllTextAsync(path, cancellationToken);

            var cached = JsonSerializer.Deserialize<BenchmarkDataset>(json);

            if (cached is null || cached.SchemaVersion != 1 || cached.CorpusVersion != "enterprise-synthetic-v1" || cached.EmbeddingModel != embeddingModel || cached.Dimensions != 1536 || cached.Chunks is null || cached.Chunks.Count != 1)
            {
                throw new InvalidOperationException($"Invalid cache metadata: {path}");
            }

            var chunk = cached.Chunks[0];

            if (chunk is null || chunk.ChunkId != chunkId || chunk.Content != passages[index] || chunk.Embedding is null || chunk.Embedding.Length != 1536 || chunk.Embedding.Any(value => !float.IsFinite(value)) || !chunk.Embedding.Any(value => value != 0))
            {
                throw new InvalidOperationException($"Invalid cache content: {path}");
            }

            chunks.Add(chunk);
        }

        return new BenchmarkDataset(1, "enterprise-synthetic-v1", embeddingModel, 1536, chunks);
    }
}