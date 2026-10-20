using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Application.Retrieval;
using AInDotNet.RagDemo.Data.Models;
using AInDotNet.RagDemo.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Testing.Platform.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public class ClaimExtractionIntegrationTests
{
    public TestContext TestContext { get; set; } = null!;
    private readonly IHost host;

    public ClaimExtractionIntegrationTests()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets<ClaimExtractionIntegrationTests>();
        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");
        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";
        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());
        builder.Services.AddTransient<IClaimExtractor, LlmClaimExtractor>();
        host = builder.Build();
    }

    [TestMethod]
    public async Task ExtractClaimsAsync_FixedAnswer_ReportsClaims()
    {
        const string answer = """
            The retrieved sources do not provide specific information
            about which authorization controls are currently implemented
            in the RAG demonstration application. They recommend that
            enterprise RAG systems must enforce authorization, data
            classification, source attribution, and auditing, emphasizing
            that document access should not be granted solely based on
            semantic relevance [S1]. However, no passage confirms which
            authorization controls are in place in the actual
            demonstration application.
            """;

        // Arrange
        //var builder = Host.CreateApplicationBuilder();

        //builder.Configuration.AddUserSecrets<RagAnswerIntegrationTests>();

        //string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        //string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        //builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        //builder.Services.AddTransient<IClaimExtractor, LlmClaimExtractor>();

        //using var host = builder.Build();

        IClaimExtractor extractor = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IClaimExtractor>(host.Services);
        using var scope = host.Services.CreateScope();

        IChatClient chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(scope.ServiceProvider);

        // do work
        var claims = await extractor.ExtractClaimsAsync(answer);

        TestContext.WriteLine($"Extracted claims: {claims.Count}");

        foreach (var claim in claims)
        {
            TestContext.WriteLine($"Claim: {claim}");
        }

        Assert.IsTrue(claims.Count > 0);
    }

    [TestMethod]
    public async Task ExtractAsync_CitedClaims_ReportsRelationships()
    {
        // Arrange
        const string answer = """
        Enterprise RAG systems must enforce authorization [S1].
        OAuth 2.0 provides delegated authorization [S5].
        The retrieved sources do not establish which controls
        are implemented in our demonstration application.
        """;

        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);

        var extractor = new LlmCitedClaimExtractor(chatClient);

        // Act
        var citedClaims = await extractor.ExtractAsync(answer);

        // Report
        TestContext.WriteLine($"Extracted claims: {citedClaims.Count}");

        foreach (var item in citedClaims)
        {
            TestContext.WriteLine($"Claim: {item.Claim}");
            TestContext.WriteLine($"Citations: {string.Join(", ", item.CitationLabels)}");
        }

        // Assert basic execution contract
        Assert.AreEqual(3, citedClaims.Count);
        CollectionAssert.AreEquivalent(new[] { "S1" }, citedClaims[0].CitationLabels.ToArray());

        CollectionAssert.AreEquivalent(new[] { "S5" }, citedClaims[1].CitationLabels.ToArray());

        Assert.AreEqual(0, citedClaims[2].CitationLabels.Count, "The uncited claim must remain uncited.");
    }

    [TestMethod]
    public async Task VerifyAsync_ValidClaimSpan_ReturnsTrue()
    {
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);
        var verifier = new LlmClaimSpanVerifier(chatClient);

        const string claim = "Enterprise RAG requires authorization.";

        const string originalText = "Enterprise RAG systems must enforce authorization [S1].";

        bool result = await verifier.VerifyAsync(claim, originalText);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task VerifyAsync_InvalidClaimSpan_ReturnsFalse()
    {
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);

        var verifier = new LlmClaimSpanVerifier(chatClient);

        const string claim = "Enterprise RAG requires authorization.";

        const string originalText = "OAuth 2.0 provides delegated authorization [S5].";

        bool result = await verifier.VerifyAsync(claim, originalText);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_ValidCitation_ReturnsSupported()
    {
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);

        var extractor = new LlmCitedClaimExtractor(chatClient);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient));

        var evidenceVerifier = new LlmClaimEvidenceVerifier(chatClient);

        var verifier = new CitationVerifier(extractor, semanticValidator, evidenceVerifier, new FakeClaimClassifier());

        var source = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-CITATION-001",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG systems must enforce authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(1, result.SupportedCitations);
        Assert.AreEqual(0, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(1.0, result.CitationAccuracy);
    }

    [TestMethod]
    public async Task VerifyAsync_UnsupportedCitation_ReturnsUnsupported()
    {
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);

        var extractor = new LlmCitedClaimExtractor(chatClient);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient));

        var evidenceVerifier = new LlmClaimEvidenceVerifier(chatClient);

        var verifier = new CitationVerifier(extractor, semanticValidator, evidenceVerifier, new FakeClaimClassifier());

        var source = new RagSource(
            "S1",
            101,
            10,
            "Vector Search Architecture",
            "Vector search retrieves semantically similar document chunks.",
            0.25);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-CITATION-002",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG systems must enforce authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(1, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);

        Assert.AreEqual(CitationVerificationOutcome.Unsupported, result.Citations[0].Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_UnknownCitation_ReturnsUnknownSource()
    {
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);

        var extractor = new LlmCitedClaimExtractor(chatClient);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient));

        var evidenceVerifier = new LlmClaimEvidenceVerifier(chatClient);

        var verifier = new CitationVerifier(extractor, semanticValidator, evidenceVerifier, new FakeClaimClassifier());

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-CITATION-003",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG systems must enforce authorization [S9].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(0, result.UnsupportedCitations);
        Assert.AreEqual(1, result.UnknownSourceCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);

        Assert.AreEqual("S9", result.Citations[0].SourceLabel);

        Assert.AreEqual(CitationVerificationOutcome.UnknownSource, result.Citations[0].Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_MixedCitations_ReturnsFiftyPercentAccuracy()
    {
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);

        var verifier = new CitationVerifier(new LlmCitedClaimExtractor(chatClient), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(chatClient)), new LlmClaimEvidenceVerifier(chatClient), new FakeClaimClassifier());

        var source1 = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var source5 = new RagSource(
            "S5",
            105,
            20,
            "Vector Search Architecture",
            "Vector search retrieves semantically similar document chunks.",
            0.35);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "LIVE-CITATION-004",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG systems must enforce authorization [S1, S5].",
            RetrievedSources = [source1, source5],
            CitedSources = [source1, source5],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 2
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(2, result.TotalCitations);
        Assert.AreEqual(1, result.SupportedCitations);
        Assert.AreEqual(1, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(0.5, result.CitationAccuracy);

        Assert.IsTrue(result.Citations.Any(c => c.SourceLabel == "S1" && c.Outcome == CitationVerificationOutcome.Supported));

        Assert.IsTrue(result.Citations.Any(c => c.SourceLabel == "S5" && c.Outcome == CitationVerificationOutcome.Unsupported));
    }

    [TestMethod]
    public async Task PassageClaimExtraction_ValidPassages_ReturnsCorrectIds()
    {
        // Arrange
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);
        const string answer = "The retrieved sources do not identify implemented authorization controls.\n" + "Enterprise RAG systems must enforce authorization [S1].";

        var passages = CitedPassageExtractor.Extract(answer);

        var extractor = new LlmPassageClaimExtractor(chatClient);

        // Act
        var claims = await extractor.ExtractAsync(passages);

        // Assert
        Assert.IsTrue(claims.Count > 0);

        Assert.IsTrue(claims.All(c => passages.Any(p => p.PassageId == c.PassageId)), "Every extracted claim must reference an existing passage.");

        Assert.IsTrue(claims.Any(c => c.PassageId == 2), "The authorization claim should reference passage 2.");

        foreach (var claim in claims)
        {
            Console.WriteLine($"Passage {claim.PassageId}: {claim.Claim}");
        }

        // Resolve the authorization claim to its original passage.
        var authorizationClaim = claims.First(c => c.PassageId == 2);

        var resolved = PassageClaimResolver.Resolve(authorizationClaim, passages);

        Assert.AreEqual("Enterprise RAG systems must enforce authorization [S1].", resolved.OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, resolved.CitationLabels.ToArray());

        Console.WriteLine();
        Console.WriteLine("RESOLVED CITATION:");
        Console.WriteLine($"Claim: {resolved.Claim}");
        Console.WriteLine($"Original text: {resolved.OriginalText}");
        Console.WriteLine($"Citation labels: {string.Join(", ", resolved.CitationLabels)}");
    }

    [TestMethod]
    public async Task PassageClaimExtraction_MultipleCitedAssertions_MapsCorrectCitations()
    {
        // Arrange
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);
        const string answer = "Enterprise RAG requires authorization [S1], " + "and supports auditing [S2, S3].";

        var passages = CitedPassageExtractor.Extract(answer);

        var extractor = new LlmPassageClaimExtractor(chatClient);

        // Act
        var claims = await extractor.ExtractAsync(passages);

        var resolved = claims.Select(claim => PassageClaimResolver.Resolve(claim, passages)).ToList();

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.IsTrue(resolved.Any(c => c.CitationLabels.SequenceEqual(new[] { "S1" })), "Expected an authorization claim citing S1.");

        Assert.IsTrue(resolved.Any(c => c.CitationLabels.SequenceEqual(new[] { "S2", "S3" })), "Expected an auditing claim citing S2 and S3.");

        foreach (var claim in resolved)
        {
            Console.WriteLine($"Claim: {claim.Claim}");
            Console.WriteLine($"Citations: {string.Join(", ", claim.CitationLabels)}");
            Console.WriteLine($"Original: {claim.OriginalText}");
            Console.WriteLine();
        }
    }

    [TestMethod]
    public async Task PassageClaimExtraction_UncitedClaim_PreservesMissingCitation()
    {
        // Arrange
        var chatClient = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IChatClient>(host.Services);
        const string answer = "Enterprise RAG requires authorization [S1]. " + "The application encrypts all documents.";

        var passages = CitedPassageExtractor.Extract(answer);

        var extractor = new LlmPassageClaimExtractor(chatClient);

        // Act
        var claims = await extractor.ExtractAsync(passages);

        var resolved = claims.Select(claim => PassageClaimResolver.Resolve(claim, passages)).ToList();

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.IsTrue(resolved.Any(c => c.CitationLabels.SequenceEqual(new[] { "S1" })), "Expected a cited authorization claim.");

        Assert.IsTrue(resolved.Any(c => c.OriginalText.Contains("encrypts all documents", StringComparison.Ordinal) && c.CitationLabels.Count == 0), "Expected an uncited encryption claim.");

        foreach (var claim in resolved)
        {
            Console.WriteLine($"Claim: {claim.Claim}");
            Console.WriteLine($"Citations: {string.Join(", ", claim.CitationLabels)}");
            Console.WriteLine($"Original: {claim.OriginalText}");
            Console.WriteLine();
        }
    }
}