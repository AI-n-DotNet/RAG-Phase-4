using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using System.Diagnostics;
using AInDotNet.RagDemo.Data.Models;

namespace AInDotNet.RagDemo.Tests.Performance;


[TestClass]
[TestCategory("Performance")]
[DoNotParallelize]
public sealed class SqlRetrievalPerformanceTests
{
    [TestMethod]
    public async Task ExactSearch_CurrentDatabase_ReportsLatency()
    {
        const int topK = 5;
        const int warmupCount = 3;
        const int measuredCount = 30;

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets<SqlRetrievalPerformanceTests>();

        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is missing.");

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string model = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        builder.Services.AddDbContext<AInDotNetRAGContext>(options => options.UseSqlServer(connectionString));

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(apiKey).GetEmbeddingClient(model).AsIEmbeddingGenerator());

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AInDotNetRAGContext>();

        var generator = scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        int chunkCount = await db.DocumentChunks.CountAsync();

        Assert.IsTrue(chunkCount > 0, "The benchmark requires an existing document corpus.");

        // One OpenAI call, outside the measured SQL searches.
        var embedding = await generator.GenerateAsync("Where does the AInDotNet demonstration store embeddings?");

        Assert.AreEqual(1536, embedding.Vector.Length);

        var queryVector = new SqlVector<float>(embedding.Vector);

        // Match the production retrieval projection and ordering.
        var query = db.DocumentChunks
            .AsNoTracking()
            .Select(chunk => new
            {
                chunk.Id,
                chunk.DocumentId,
                DocumentTitle = chunk.Document.Title,
                chunk.Content,
                Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)
            })
            .OrderBy(result => result.Distance)
            .Take(topK);

        // Warm up query translation, execution, and database caches.
        for (int index = 0; index < warmupCount; index++)
        {
            await query.ToListAsync();
        }

        var durations = new List<double>(measuredCount);

        for (int index = 0; index < measuredCount; index++)
        {
            long started = Stopwatch.GetTimestamp();

            var results = await query.ToListAsync();

            double milliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            durations.Add(milliseconds);

            Assert.AreEqual(Math.Min(topK, chunkCount), results.Count);
        }

        durations.Sort();

        double median = (durations[measuredCount / 2 - 1] + durations[measuredCount / 2]) / 2;

        int p95Index = (int)Math.Ceiling(measuredCount * 0.95) - 1;

        Console.WriteLine("EXACT SQL RETRIEVAL BASELINE");
        Console.WriteLine($"UTC: {DateTimeOffset.UtcNow:O}");
        Console.WriteLine($"Chunks: {chunkCount}");
        Console.WriteLine($"Embedding model: {model}");
        Console.WriteLine("Dimensions: 1536");
        Console.WriteLine($"Top K: {topK}");
        Console.WriteLine("Concurrent searches: 1");
        Console.WriteLine($"Warm-up searches: {warmupCount}");
        Console.WriteLine($"Measured searches: {measuredCount}");
        Console.WriteLine($"Median: {median:F3} ms");
        Console.WriteLine($"95th percentile: {durations[p95Index]:F3} ms");
        Console.WriteLine($"Minimum: {durations[0]:F3} ms");
        Console.WriteLine($"Maximum: {durations[^1]:F3} ms");
    }
}