using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using AInDotNet.RagDemo.Tests.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public sealed class ClaimExtractionIntegrationTests : IDisposable
{
    private readonly IHost host;

    public ClaimExtractionIntegrationTests()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimExtractionIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddTransient<IClaimExtractor, LlmClaimExtractor>();
        builder.Services.AddTransient<IClaimNormalizer, LlmClaimNormalizer>();

        host = builder.Build();
    }

    public void Dispose()
    {
        host.Dispose();
    }

    [TestMethod]
    public async Task ExtractClaimsAsync_AuthorizationAnswer_ReportsClaims()
    {
        // Arrange
        var extractor = host.Services.GetRequiredService<IClaimExtractor>();

        const string answer =
            "The retrieved sources do not provide explicit information " +
            "about the authorization controls currently implemented " +
            "in your RAG demonstration application. " +
            "They discuss that enterprise RAG systems must enforce " +
            "authorization, data classification, source attribution, " +
            "and auditing, and mention OAuth 2.0 as a framework for " +
            "delegated authorization to protected resources. " +
            "However, none of the sources confirm which, if any, " +
            "authorization controls have been implemented in your " +
            "demonstration application specifically [S1][S5].";

        // Act
        var claims = await extractor.ExtractClaimsAsync(answer);

        // Report results
        Console.WriteLine($"Extracted claims: {claims.Count}");
        Console.WriteLine();

        for (int i = 0; i < claims.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {claims[i]}");
        }

        // Assert basic execution contract.
        Assert.IsTrue(claims.Count > 0, "Expected at least one extracted claim.");

        Assert.IsTrue(claims.All(c => !string.IsNullOrWhiteSpace(c)), "Extracted claims must not be empty.");
    }

    [TestMethod]
    public async Task NormalizeAsync_DuplicateClaims_ReportsResults()
    {
        // Arrange
        var claims = new List<string>
        {
        "The retrieved sources do not provide specific information " +
        "about which authorization controls are currently implemented " +
        "in the RAG demonstration application.",

        "The sources recommend that enterprise RAG systems must " +
        "enforce authorization, data classification, source " +
        "attribution, and auditing.",

        "The sources emphasize that document access should not " +
        "be granted solely based on semantic relevance.",

        "No passage confirms which authorization controls are " +
        "in place in the actual demonstration application."
        };

        var normalizer = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IClaimNormalizer>(host.Services);

        // Act
        var normalized = await normalizer.NormalizeAsync(claims);

        // Report
        Console.WriteLine($"Original claims: {claims.Count}");
        Console.WriteLine($"Normalized claims: {normalized.Count}");

        foreach (var claim in normalized)
        {
            Console.WriteLine($"Claim: {claim}");
        }

        // Assert
        Assert.IsTrue(normalized.Count > 0);
    }

    [TestMethod]
    public async Task NormalizeAsync_DistinctClaims_PreservesAllClaims()
    {
        // Arrange
        var claims = new List<string>
        {
            "Enterprise RAG systems must enforce document-level authorization.",

            "The demonstration application currently implements document-level authorization.",

            "The demonstration application does not currently implement document-level authorization.",

            "The retrieved sources do not establish whether the demonstration application implements document-level authorization."
        };

        // Act
        var normalizer = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IClaimNormalizer>(host.Services);
        var normalized = await normalizer.NormalizeAsync(claims);

        // Report
        Console.WriteLine($"Original claims: {claims.Count}");
        Console.WriteLine($"Normalized claims: {normalized.Count}");

        foreach (var claim in normalized)
        {
            Console.WriteLine($"Claim: {claim}");
        }

        // Assert
        Assert.AreEqual(4, normalized.Count, "Claims with materially different meanings must not be merged.");
    }

    [TestMethod]
    public async Task EvaluateAsync_ValidAnswer_ReturnsCombinedEvaluation()
    {
        // Arrange: Faithfulness evaluation
        IChatClient chatClient = host.Services.GetRequiredService<IChatClient>();

        var evidenceVerifier = new LlmClaimEvidenceVerifier(chatClient);

        var faithfulnessEvaluator = new FaithfulnessEvaluator(new LlmClaimExtractor(chatClient), new LlmClaimNormalizer(chatClient), evidenceVerifier);

        // Arrange: Citation verification
        var citationVerifier = new CitationVerifier(new LlmCitedClaimExtractor(chatClient), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient)), evidenceVerifier, new FakeClaimClassifier());

        // Arrange: Combined evaluator
        var evaluator = new RagEvaluator(faithfulnessEvaluator, citationVerifier);

        var source = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-EVALUATION-001",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG systems must enforce authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act
        RagEvaluationResult result = await evaluator.EvaluateAsync(evaluationRun);

        // Assert: Orchestration
        Assert.AreEqual("LIVE-EVALUATION-001", result.EvaluationCaseId);

        // Assert: Faithfulness
        Assert.AreEqual(1.0, result.Faithfulness.FaithfulnessScore);
        Assert.IsTrue(result.Faithfulness.TotalClaims >= 1);

        // Assert: Citation verification
        Assert.AreEqual(1, result.Citations.TotalClaims);
        Assert.AreEqual(1, result.Citations.CitedClaims);
        Assert.AreEqual(1.0, result.Citations.CitationCoverage);

        Assert.AreEqual(1, result.Citations.TotalCitations);
        Assert.AreEqual(1, result.Citations.SupportedCitations);
        Assert.AreEqual(1.0, result.Citations.CitationAccuracy);
    }

    [TestMethod]
    public async Task EvaluateAsync_UnsupportedAnswer_ReturnsZeroFaithfulnessAndCitationAccuracy()
    {
        // Arrange
        IChatClient chatClient = host.Services.GetRequiredService<IChatClient>();
        var evidenceVerifier = new LlmClaimEvidenceVerifier(chatClient);

        var faithfulnessEvaluator = new FaithfulnessEvaluator(new LlmClaimExtractor(chatClient), new LlmClaimNormalizer(chatClient), evidenceVerifier);

        var citationVerifier = new CitationVerifier(new LlmCitedClaimExtractor(chatClient), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient)), evidenceVerifier, new FakeClaimClassifier());

        var evaluator = new RagEvaluator(faithfulnessEvaluator, citationVerifier);

        var source = new RagSource(
            "S1",
            101,
            10,
            "Vector Search Architecture",
            "Vector search retrieves semantically similar document chunks.",
            0.25);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-EVALUATION-002",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG systems must enforce authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act
        RagEvaluationResult result = await evaluator.EvaluateAsync(evaluationRun);
        Console.WriteLine($"Total claims: {result.Faithfulness.TotalClaims}");
        Console.WriteLine($"Supported claims: {result.Faithfulness.SupportedClaims}");
        Console.WriteLine($"Faithfulness score: {result.Faithfulness.FaithfulnessScore}");

        // Assert: Faithfulness
        Assert.IsTrue(result.Faithfulness.TotalClaims >= 1);
        Assert.AreEqual(0, result.Faithfulness.SupportedClaims);
        Assert.AreEqual(0.0, result.Faithfulness.FaithfulnessScore);

        // Assert: Citation coverage
        Assert.AreEqual(1, result.Citations.TotalClaims);
        Assert.AreEqual(1, result.Citations.CitedClaims);
        Assert.AreEqual(1.0, result.Citations.CitationCoverage);

        // Assert: Citation accuracy
        Assert.AreEqual(1, result.Citations.TotalCitations);
        Assert.AreEqual(0, result.Citations.SupportedCitations);
        Assert.AreEqual(1, result.Citations.UnsupportedCitations);
        Assert.AreEqual(0.0, result.Citations.CitationAccuracy);
    }

    [TestMethod]
    public async Task EvaluateAsync_UncitedClaim_ReturnsPartialCoverage()
    {
        // Arrange
        IChatClient chatClient = host.Services.GetRequiredService<IChatClient>();
        var evidenceVerifier = new LlmClaimEvidenceVerifier(chatClient);

        var faithfulnessEvaluator = new FaithfulnessEvaluator(new LlmClaimExtractor(chatClient), new LlmClaimNormalizer(chatClient), evidenceVerifier);

        var citationVerifier = new CitationVerifier(new LlmCitedClaimExtractor(chatClient), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient)), evidenceVerifier, new FakeClaimClassifier());

        var evaluator = new RagEvaluator(faithfulnessEvaluator, citationVerifier);

        var source1 = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var source2 = new RagSource(
            "S2",
            102,
            20,
            "OAuth Architecture",
            "OAuth 2.0 provides delegated authorization.",
            0.35);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-EVALUATION-003",
            Question = "What authorization mechanisms are relevant?",
            Answer = """
            Enterprise RAG systems must enforce authorization [S1].
            OAuth 2.0 provides delegated authorization.
            """,
            RetrievedSources = [source1, source2],
            CitedSources = [source1],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 2
        };

        // Act
        RagEvaluationResult result = await evaluator.EvaluateAsync(evaluationRun);

        // Assert: Faithfulness
        Assert.IsTrue(result.Faithfulness.TotalClaims >= 1);
        Assert.AreEqual(1.0, result.Faithfulness.FaithfulnessScore);

        // Assert: Citation coverage
        Assert.AreEqual(2, result.Citations.TotalClaims);
        Assert.AreEqual(1, result.Citations.CitedClaims);
        Assert.AreEqual(0.5, result.Citations.CitationCoverage);

        // Assert: Citation accuracy
        Assert.AreEqual(1, result.Citations.TotalCitations);
        Assert.AreEqual(1, result.Citations.SupportedCitations);
        Assert.AreEqual(1.0, result.Citations.CitationAccuracy);
    }

    [TestMethod]
    public async Task ExtractClaimsAsync_MixedEvidenceAndRequirement_ExtractsThreeClaims()
    {
        // Arrange
        IChatClient chatClient = host.Services.GetRequiredService<IChatClient>();

        var extractor = new LlmClaimExtractor(chatClient);

        string answer = """
        The retrieved sources do not establish which, if any,
        authorization controls are currently implemented in your
        RAG demonstration application. While one source recommends
        that enterprise RAG systems must enforce authorization
        among other controls [S1], no source explicitly confirms
        the actual implementation of authorization controls in
        the specific RAG demonstration application.
        """;

        // Act
        IReadOnlyList<string> claims = await extractor.ExtractClaimsAsync(answer);

        // Diagnostic output
        Console.WriteLine($"Extracted claims: {claims.Count}");

        foreach (string claim in claims)
        {
            Console.WriteLine($"CLAIM: {claim}");
        }

        // Assert
        //Assert.AreEqual(2, claims.Count, "Expected one architectural requirement claim and one deduplicated evidence-limitation claim.");
        Assert.AreEqual(3, claims.Count, "Expected an architectural requirement and two distinct evidence-limitation claims.");
    }
}