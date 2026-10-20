using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Ingestion;
using AInDotNet.RagDemo.Application.Retrieval;
using AInDotNet.RagDemo.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace AInDotNet.RagDemo.Tests.Integration;


[TestClass]
[TestCategory("Integration")]
public sealed class DocumentIngestionIntegrationTests
{
    [TestMethod]
    public async Task IngestAsync_MultipleChunks_RetrievesFactNearEnd()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets<DocumentIngestionIntegrationTests>();

        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is missing.");

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string embeddingModel = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        builder.Services.AddDbContext<AInDotNetRAGContext>(options => options.UseSqlServer(connectionString));

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(apiKey).GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

        builder.Services.AddSingleton<DocumentChunkingService>();
        builder.Services.AddScoped<DocumentIngestionService>();
        builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AInDotNetRAGContext>();

        var ingestion = scope.ServiceProvider.GetRequiredService<DocumentIngestionService>();

        var search = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();

        const string targetFact = "Project Cedar keeps its emergency recovery keys " + "in the Northbridge vault.";

        // Exceed the default 1,500-character chunk size.
        // Put the distinctive fact at the end of the document.
        string background = string.Join("\n\n", Enumerable.Range(1, 20).Select(index => $"Section {index}: The gardening handbook describes " + "seasonal planting, soil preparation, watering schedules, " + "and routine maintenance of vegetable beds."));

        string content = background + "\n\n" + targetFact;

        // Keep this test's document separate from other test data.
        string sourceUri = $"test://multichunk/{Guid.NewGuid():N}";

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            DocumentIngestionResult result = await ingestion.IngestAsync("Multi-chunk ingestion test", sourceUri, content);

            // Reload persisted rows rather than inspecting tracked entities.
            db.ChangeTracker.Clear();

            var chunks = await db.DocumentChunks
                .AsNoTracking()
                .Where(chunk => chunk.DocumentId == result.DocumentId)
                .OrderBy(chunk => chunk.ChunkIndex)
                .ToListAsync();

            Assert.IsTrue(result.WasInserted);
            Assert.IsTrue(chunks.Count > 1);
            Assert.AreEqual(result.ChunkCount, chunks.Count);

            CollectionAssert.AreEqual(Enumerable.Range(0, chunks.Count).ToArray(), chunks.Select(chunk => chunk.ChunkIndex).ToArray());

            Assert.IsFalse(chunks[0].Content.Contains(targetFact, StringComparison.Ordinal));

            Assert.IsTrue(chunks[^1].Content.Contains(targetFact, StringComparison.Ordinal));

            var matches = await search.SearchAsync("Where does Project Cedar keep its emergency recovery keys?", topK: 5);

            Assert.IsTrue(matches.Any(match =>
                    match.DocumentId == result.DocumentId &&
                    match.Content.Contains(targetFact, StringComparison.Ordinal)), "The top five results should include the final fact " + "from the newly ingested document.");

            Console.WriteLine($"Stored chunks: {chunks.Count}");

            foreach (var match in matches)
            {
                Console.WriteLine(
                    $"Document {match.DocumentId}, " +
                    $"Chunk {match.ChunkId}, " +
                    $"Distance {match.Distance:F6}");
            }
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}