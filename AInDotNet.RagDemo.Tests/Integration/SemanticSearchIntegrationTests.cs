using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Retrieval;
using AInDotNet.RagDemo.Data.Models;
using AInDotNet.RagDemo.Evaluation;
using AInDotNet.RagDemo.Tests.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace AInDotNet.RagDemo.Tests.Integration;


[TestClass]
[TestCategory("Integration")]
public sealed class SemanticSearchIntegrationTests
{
    [TestMethod]
    public async Task SearchAsync_AuthorizationQuestion_EvaluatesRetrieval()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<SemanticSearchIntegrationTests>();

        builder.Services.AddDbContext<AInDotNetRAGContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is missing.")));

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string embeddingModel = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(apiKey).GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

        builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var searchService = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();

        var evaluationCase = RagEvaluationCases.Authorization;

        const int topK = 5;

        // Act
        var searchResults = await searchService.SearchAsync(evaluationCase.Question, topK);

        var retrievedChunkIds = searchResults.Select(result => result.ChunkId).ToList();

        var evaluation = RetrievalEvaluator.Evaluate(evaluationCase, retrievedChunkIds, topK);

        // Report results
        Console.WriteLine($"Evaluation: {evaluation.EvaluationCaseId}");
        Console.WriteLine($"Question: {evaluationCase.Question}");
        Console.WriteLine();

        int rank = 1;

        foreach (var result in searchResults)
        {
            Console.WriteLine($"Rank {rank++}: " + $"Chunk {result.ChunkId}, " + $"Distance {result.Distance:F6}, " + $"{result.DocumentTitle}");
        }

        Console.WriteLine();
        Console.WriteLine($"Precision@{topK}: {evaluation.PrecisionAtK:F4}");
        Console.WriteLine($"Recall@{topK}: {evaluation.RecallAtK:F4}");
        Console.WriteLine($"Reciprocal Rank: {evaluation.ReciprocalRank:F4}");

        // Verify the integration returned results.
        Assert.IsNotEmpty(searchResults);
        Assert.AreEqual(topK, evaluation.TopK);

        Assert.IsTrue(searchResults.Count <= topK, "Search must not return more than the requested number of chunks.");

        Assert.AreEqual(searchResults.Count, searchResults.Select(result => result.ChunkId).Distinct().Count(), "Each retrieved chunk must appear only once.");

        Assert.IsTrue(searchResults.All(result => double.IsFinite(result.Distance)), "Every result must have a finite distance.");

        for (int index = 1; index < searchResults.Count; index++)
        {
            Assert.IsTrue(searchResults[index - 1].Distance <= searchResults[index].Distance, "Results must be ordered from lowest to highest cosine distance.");
        }
    }
}