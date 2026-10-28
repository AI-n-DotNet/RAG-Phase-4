using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Benchmarks;

[TestClass]
public sealed class BenchmarkDatasetTests
{
    [TestMethod]
    public async Task JsonRoundTrip_PreservesMetadataAndEmbeddingValues()
    {
        float[] vector = new float[1536];
        vector[0] = 0.25f;
        vector[768] = -0.5f;
        vector[1535] = 0.75f;

        var expected = new BenchmarkDataset(
            SchemaVersion: 1,
            CorpusVersion: "enterprise-synthetic-v1",
            EmbeddingModel: "text-embedding-3-small",
            Dimensions: 1536,
            Chunks:
            [
                new BenchmarkDatasetChunk(1, "Synthetic benchmark passage.", vector)
            ]);

        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(expected));

            string json = await File.ReadAllTextAsync(path);

            var actual = JsonSerializer.Deserialize<BenchmarkDataset>(json);

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.SchemaVersion, actual.SchemaVersion);
            Assert.AreEqual(expected.CorpusVersion, actual.CorpusVersion);
            Assert.AreEqual(expected.EmbeddingModel, actual.EmbeddingModel);
            Assert.AreEqual(expected.Dimensions, actual.Dimensions);
            Assert.AreEqual(1, actual.Chunks.Count);

            Assert.AreEqual(1, actual.Chunks[0].ChunkId);
            Assert.AreEqual(expected.Chunks[0].Content, actual.Chunks[0].Content);

            CollectionAssert.AreEqual(vector, actual.Chunks[0].Embedding);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}