using AInDotNet.RagDemo.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("LiveEvaluation")]
[TestCategory("ExternalDependency")]
public sealed class LlmPassageClaimExtractorTests
{
    private readonly IChatClient _chatClient;

    public LlmPassageClaimExtractorTests()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimExtractionIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddTransient<IClaimExtractor, LlmClaimExtractor>();
        builder.Services.AddTransient<IClaimNormalizer, LlmClaimNormalizer>();

        using var host = builder.Build();
        _chatClient = host.Services.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    public async Task ExtractAsync_RetrievedEvidenceAbsence_PreservesScope()
    {
        var passages = new List<CitedPassage>
    {
        new(PassageId: 1,
            OriginalText:
                "The retrieved sources provide no indication that " +
                "authorization controls are actively implemented " +
                "in the demonstration application.",
            CitationLabels: Array.Empty<string>())
    };

        var extractor = new LlmPassageClaimExtractor(_chatClient);

        IReadOnlyList<PassageClaim> claims = await extractor.ExtractAsync(passages);

        foreach (var claim in claims)
        {
            Console.WriteLine($"EXTRACTED CLAIM: {claim.Claim}");
        }

        Assert.AreEqual(1, claims.Count);

        var classifier = new LlmClaimClassifier(_chatClient);

        ClaimType claimType = await classifier.ClassifyAsync(claims[0].Claim);

        Console.WriteLine($"CLAIM TYPE: {claimType}");

        Assert.AreEqual(ClaimType.CollectionEvidence, claimType);
    }

    [TestMethod]
    public async Task ExtractAsync_AmbiguousImplementationStatus_DoesNotAssertNonImplementation()
    {
        var passages = new List<CitedPassage>
        {
            new(PassageId: 1,
                OriginalText:
                    "There is no indication that authorization controls " +
                    "are actively implemented in your demonstration application.",
                CitationLabels: Array.Empty<string>())
        };

        var extractor = new LlmPassageClaimExtractor(_chatClient);

        IReadOnlyList<PassageClaim> claims = await extractor.ExtractAsync(passages);

        foreach (var claim in claims)
        {
            Console.WriteLine($"EXTRACTED CLAIM: {claim.Claim}");

            var verifier = new LlmClaimEntailmentVerifier(_chatClient);

            ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim.Claim, passages[0].OriginalText);

            Console.WriteLine($"ENTAILMENT OUTCOME: {outcome}");

            Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
        }

        Assert.AreEqual(1, claims.Count);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ExtractAsync_ImpersonalReporting_DoesNotInventSourceAttribution()
    {
        const string original =
            "While it is noted that enterprise RAG systems must enforce authorization " +
            "among other controls [S1], there is no explicit information about which " +
            "authorization mechanisms your demonstration application uses.";
        IReadOnlyList<CitedPassage> passages =
        [
            new(PassageId: 1, OriginalText: original, CitationLabels: ["S1"])
        ];
        var extractor = new LlmPassageClaimExtractor(_chatClient);
        var verifier = new LlmClaimEntailmentVerifier(_chatClient, Console.WriteLine);

        var claims = await extractor.ExtractAsync(passages);

        Assert.IsTrue(claims.Count > 0, "The passage contains substantive assertions.");

        foreach (var claim in claims)
        {
            Assert.AreEqual(1, claim.PassageId);
            Console.WriteLine($"EXTRACTED CLAIM: {claim.Claim}");

            var outcome = await verifier.VerifyAsync(claim.Claim, original, original);

            Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome,
                $"The extracted claim must preserve the passage's reporting form: {claim.Claim}");
        }
    }
}
