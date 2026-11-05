using AInDotNet.RagDemo.Application.Evaluation;
using AInDotNet.RagDemo.Application.Generation;
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
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Integration;


[TestClass]
[TestCategory("Integration")]
public sealed class RagAnswerIntegrationTests
{
    [TestMethod]
    public async Task AnswerAsync_AuthorizationImplementation_ReportsAnswer()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<RagAnswerIntegrationTests>();

        builder.Services.AddDbContext<AInDotNetRAGContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is missing.")));

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string embeddingModel = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(apiKey).GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();

        builder.Services.AddScoped<RagAnswerService>();

        builder.Services.AddTransient<IClaimExtractor, LlmClaimExtractor>();

        builder.Services.AddTransient<IClaimEvidenceVerifier, LlmClaimEvidenceVerifier>();

        builder.Services.AddTransient<FaithfulnessEvaluator>();
        builder.Services.AddTransient<IClaimNormalizer, LlmClaimNormalizer>();

        builder.Services.AddTransient<IFaithfulnessEvaluator, FaithfulnessEvaluator>();

        builder.Services.AddTransient<IPassageClaimExtractor, LlmPassageClaimExtractor>();

        builder.Services.AddTransient<ICitedClaimExtractor, PassageBasedCitedClaimExtractor>();

        builder.Services.AddTransient<IClaimEntailmentVerifier, LlmClaimEntailmentVerifier>();

        builder.Services.AddTransient<IClaimSpanVerifier, ClaimEntailmentSpanVerifierAdapter>();

        builder.Services.AddTransient<CitedClaimSemanticValidator>();

        builder.Services.AddTransient<IClaimClassifier, LlmClaimClassifier>();

        builder.Services.AddTransient<ICitationVerifier, CitationVerifier>();

        builder.Services.AddTransient<IRagEvaluator, RagEvaluator>();

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        // Verify that the services are registered correctly.
        var citationVerifier = scope.ServiceProvider.GetRequiredService<ICitationVerifier>();
        Assert.IsNotNull(citationVerifier);

        var ragService = scope.ServiceProvider.GetRequiredService<RagAnswerService>();

        var evaluationCase = RagEvaluationCases.AuthorizationImplementation;

        // Act
        var result = await ragService.AnswerAsync(evaluationCase.Question, topK: 5);

        // Capture the actual RAG execution.
        //var evaluationRun = new RagEvaluationRun
        //{
        //    EvaluationCaseId = evaluationCase.Id,
        //    Question = evaluationCase.Question,
        //    Answer = result.Answer,
        //    RetrievedSources = result.RetrievedSources.ToArray(),
        //    CitedSources = result.Sources.ToArray(),
        //    ExecutedAtUtc = DateTimeOffset.UtcNow,
        //    TopK = 5
        //};

        // Capture the actual RAG execution.
        RagEvaluationRun evaluationRun = RagEvaluationRunFactory.Create(evaluationCase.Id, evaluationCase.Question, result, topK: 5);

        //var faithfulnessEvaluator = host.Services.GetRequiredService<FaithfulnessEvaluator>();

        //var faithfulnessResult = await faithfulnessEvaluator.EvaluateAsync(evaluationRun);

        var ragEvaluator = scope.ServiceProvider.GetRequiredService<IRagEvaluator>();

        var evaluationResult = await ragEvaluator.EvaluateAsync(evaluationRun);

        var faithfulnessResult = evaluationResult.Faithfulness;
        var citationResult = evaluationResult.Citations;

        Console.WriteLine();
        Console.WriteLine("CITATION EVALUATION:");
        Console.WriteLine($"Total citations: {citationResult.TotalCitations}");
        Console.WriteLine($"Supported citations: {citationResult.SupportedCitations}");
        Console.WriteLine($"Unsupported citations: {citationResult.UnsupportedCitations}");
        Console.WriteLine($"Unknown-source citations: {citationResult.UnknownSourceCitations}");
        Console.WriteLine($"Citation accuracy: {citationResult.CitationAccuracy:P1}");
        Console.WriteLine($"Cited-claim support rate: {citationResult.CitedClaimSupportRate:P1}");
        Console.WriteLine($"Citation coverage: {citationResult.CitationCoverage:P1}");
        Console.WriteLine($"Total factual claims: {citationResult.TotalFactualClaims}");
        Console.WriteLine($"Cited factual claims: {citationResult.CitedFactualClaims}");
        Console.WriteLine($"Factual citation coverage: {citationResult.FactualCitationCoverage:P1}");

        Console.WriteLine();
        Console.WriteLine("CITATION COVERAGE DETAILS:");
        Console.WriteLine($"Total claims: {citationResult.TotalClaims}");
        Console.WriteLine($"Cited claims: {citationResult.CitedClaims}");
        Console.WriteLine($"Reported coverage: {citationResult.CitationCoverage:P1}");
        Console.WriteLine($"Calculated coverage: " + $"{(citationResult.TotalClaims == 0 ? 0.0 : (double)citationResult.CitedClaims / citationResult.TotalClaims):P1}");

        Console.WriteLine();
        Console.WriteLine("INDIVIDUAL CITATION RESULTS:");

        foreach (var citation in citationResult.Citations)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine($"Claim: {citation.Claim}");
            Console.WriteLine($"Source: {citation.SourceLabel}");
            Console.WriteLine($"Outcome: {citation.Outcome}");
            Console.WriteLine($"Explanation: {citation.Explanation ?? "(none)"}");
        }

        Console.WriteLine();
        Console.WriteLine("RETRIEVAL EVALUATION:");

        Console.WriteLine($"Retrieved sources: {evaluationRun.RetrievedSources.Count}");

        Console.WriteLine($"Cited sources: {evaluationRun.CitedSources.Count}");

        foreach (var source in evaluationRun.RetrievedSources)
        {
            bool wasCited = evaluationRun.CitedSources.Any(cited => cited.Label == source.Label);

            Console.WriteLine($"[{source.Label}] " + $"Chunk {source.ChunkId}, " + $"Distance {source.Distance:F6}, " + $"Cited: {wasCited}");
        }

        Console.WriteLine();
        Console.WriteLine("FAITHFULNESS EVALUATION:");

        Console.WriteLine($"Total claims: {faithfulnessResult.TotalClaims}");

        Console.WriteLine($"Supported claims: {faithfulnessResult.SupportedClaims}");

        Console.WriteLine($"Faithfulness score: {faithfulnessResult.FaithfulnessScore:P1}");

        foreach (var claim in faithfulnessResult.Claims)
        {
            Console.WriteLine();
            Console.WriteLine($"Claim: {claim.Claim}");
            Console.WriteLine($"Outcome: {claim.Outcome}");

            Console.WriteLine($"Evidence: {string.Join(", ", claim.SupportingSourceLabels)}");

            Console.WriteLine($"Explanation: {claim.Explanation}");
        }

        // Report results
        Console.WriteLine($"Evaluation: {evaluationCase.Id}");
        Console.WriteLine($"Question: {evaluationCase.Question}");
        Console.WriteLine($"Should Abstain: {evaluationCase.ShouldAbstain}");
        Console.WriteLine();

        Console.WriteLine("ANSWER:");
        Console.WriteLine(result.Answer);
        Console.WriteLine();

        Console.WriteLine("CITED SOURCES:");

        foreach (var source in result.Sources)
        {
            Console.WriteLine(
                $"[{source.Label}] " +
                $"{source.DocumentTitle} " +
                $"(Chunk {source.ChunkId}, " +
                $"Distance {source.Distance:F6})");
        }

        // Verify that the execution was captured correctly.
        Assert.AreEqual(evaluationCase.Id, evaluationRun.EvaluationCaseId);

        Assert.AreEqual(evaluationCase.Question, evaluationRun.Question);

        Assert.IsFalse(string.IsNullOrWhiteSpace(evaluationRun.Answer));

        Assert.AreEqual(5, evaluationRun.TopK);

        Assert.IsTrue(evaluationRun.ExecutedAtUtc <= DateTimeOffset.UtcNow);

        Assert.AreEqual(result.Sources.Count, evaluationRun.CitedSources.Count);

        // Every cited source must belong to the retrieved set.
        var retrievedLabels = evaluationRun.RetrievedSources.Select(source => source.Label).ToHashSet(StringComparer.Ordinal);

        foreach (var citedSource in evaluationRun.CitedSources)
        {
            Assert.IsTrue(retrievedLabels.Contains(citedSource.Label), $"Cited source {citedSource.Label} was not retrieved.");
        }

        // The complete retrieval context must be preserved.
        Assert.AreEqual(result.RetrievedSources.Count, evaluationRun.RetrievedSources.Count);

        // The cited subset must not exceed the retrieved set.
        Assert.IsTrue(evaluationRun.CitedSources.Count <= evaluationRun.RetrievedSources.Count);

        Assert.IsTrue(faithfulnessResult.TotalClaims > 0, "The evaluator should extract at least one claim.");

        Assert.IsTrue(faithfulnessResult.FaithfulnessScore >= 0 && faithfulnessResult.FaithfulnessScore <= 1, "Faithfulness score must be between 0 and 1.");
    }
}