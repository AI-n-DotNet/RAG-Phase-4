using AInDotNet.RagDemo.BenchmarkData.Models;
using AInDotNet.RagDemo.Benchmarks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Benchmarks;

[TestClass]
[TestCategory("Integration")]
public sealed class LegacyVectorLoaderTests
{
    [TestMethod]
    public async Task LoadAsync_StoredJson_PreservesIdAndVectorValues()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<LegacyVectorLoaderTests>().Build();

        string connectionString = configuration.GetConnectionString("BenchmarkConnection") ?? throw new InvalidOperationException("BenchmarkConnection is missing.");

        var options = new DbContextOptionsBuilder<AInDotNetRAG_BenchmarkContext>().UseSqlServer(connectionString).Options;

        await using var db = new AInDotNetRAG_BenchmarkContext(options);

        Assert.AreEqual("AInDotNetRAG_Benchmark", db.Database.GetDbConnection().Database, "This test must use the separate benchmark database.");

        int id = -Random.Shared.Next(1, int.MaxValue);

        float[] expected = new float[1536];
        expected[0] = 0.25f;
        expected[768] = -0.5f;
        expected[1535] = 0.75f;

        string json = JsonSerializer.Serialize(expected);

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""INSERT INTO dbo.LegacyBenchmarkChunks(Id, Content, EmbeddingJson) VALUES ({id}, {"Loader test"}, {json}) """);

            var vectors = await LegacyVectorLoader.LoadAsync(db);
            var actual = vectors.Single(vector => vector.ChunkId == id);

            CollectionAssert.AreEqual(expected, actual.Embedding);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [TestMethod]
    public async Task NativeAndJsonStorage_PreserveIdenticalVectorsAndIds()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<LegacyVectorLoaderTests>().Build();

        string connectionString = configuration.GetConnectionString("BenchmarkConnection") ?? throw new InvalidOperationException("BenchmarkConnection is missing.");

        var options = new DbContextOptionsBuilder<AInDotNetRAG_BenchmarkContext>().UseSqlServer(connectionString).Options;

        await using var db = new AInDotNetRAG_BenchmarkContext(options);

        Assert.AreEqual("AInDotNetRAG_Benchmark", db.Database.GetDbConnection().Database);

        // Synthetic values verify storage without an OpenAI call.
        // Real benchmark data will use actual text embeddings.
        float[] expected = new float[1536];
        expected[0] = 0.25f;
        expected[768] = -0.5f;
        expected[1535] = 0.75f;

        const string content = "Paired vector storage verification.";

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var document = new Document
            {
                Title = "Paired storage test",
                SourceUri = $"test://paired-storage/{Guid.NewGuid():N}"
            };

            var chunk = new DocumentChunk
            {
                ChunkIndex = 0,
                Content = content,
                Embedding = new Microsoft.Data.SqlTypes.SqlVector<float>(expected)
            };

            document.DocumentChunks.Add(chunk);
            db.Documents.Add(document);

            await db.SaveChangesAsync();

            // Reuse the native chunk's generated ID for its JSON counterpart.
            int chunkId = chunk.Id;

            db.LegacyBenchmarkChunks.Add(new LegacyBenchmarkChunk
            {
                Id = chunkId,
                Content = content,
                EmbeddingJson = JsonSerializer.Serialize(expected)
            });

            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var native = await db.DocumentChunks.AsNoTracking().SingleAsync(item => item.Id == chunkId);

            var legacy = await db.LegacyBenchmarkChunks.AsNoTracking().SingleAsync(item => item.Id == chunkId);

            var loadedVectors = await LegacyVectorLoader.LoadAsync(db);
            var loaded = loadedVectors.Single(item => item.ChunkId == chunkId);

            Assert.AreEqual(native.Id, legacy.Id);
            Assert.AreEqual(native.Content, legacy.Content);

            CollectionAssert.AreEqual(expected, native.Embedding.Memory.ToArray());

            CollectionAssert.AreEqual(native.Embedding.Memory.ToArray(), loaded.Embedding);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}