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
}
