using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using AInDotNet.RagDemo.Tests.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace AInDotNet.RagDemo.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public sealed class ClaimEvidenceVerificationIntegrationTests
{
    [TestMethod]
    public async Task VerifyAsync_RequirementVersusImplementation_DistinguishesEvidence()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimEvidenceVerificationIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddTransient<IClaimEvidenceVerifier, LlmClaimEvidenceVerifier>();

        using var host = builder.Build();

        var verifier = host.Services.GetRequiredService<IClaimEvidenceVerifier>();

        // Fixed evidence from Chunk 69.
        IReadOnlyList<RagSource> sources =
        [
            new(
                "S1",
                69,
                1,
                "Enterprise RAG Architecture",
                "Enterprise RAG systems must also enforce " +
                "authorization, data classification, source " +
                "attribution, and auditing. A document should " +
                "not become accessible to a user merely because " +
                "it is semantically relevant.",
                0.373208)
        ];

        const string requirementClaim = "Enterprise RAG systems must enforce authorization.";

        const string implementationClaim = "Our RAG demonstration application currently enforces document-level authorization.";

        // Act
        var requirementResult = await verifier.VerifyAsync(requirementClaim, sources);

        var implementationResult = await verifier.VerifyAsync(implementationClaim, sources);

        // Report
        Console.WriteLine("REQUIREMENT CLAIM:");
        Console.WriteLine($"Outcome: {requirementResult.Outcome}");
        Console.WriteLine($"Explanation: {requirementResult.Explanation}");
        Console.WriteLine();

        Console.WriteLine("IMPLEMENTATION CLAIM:");
        Console.WriteLine($"Outcome: {implementationResult.Outcome}");
        Console.WriteLine($"Explanation: {implementationResult.Explanation}");

        // Assert
        Assert.AreEqual(ClaimSupportOutcome.Supported, requirementResult.Outcome);

        CollectionAssert.Contains(requirementResult.SupportingSourceLabels, "S1");

        Assert.AreEqual(ClaimSupportOutcome.InsufficientEvidence, implementationResult.Outcome, "The evidence does not establish implementation.");
    }

    [TestMethod]
    public async Task VerifyAsync_ExplicitContradiction_ReportsContradicted()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimEvidenceVerificationIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddTransient<IClaimEvidenceVerifier, LlmClaimEvidenceVerifier>();

        using var host = builder.Build();

        var verifier = host.Services.GetRequiredService<IClaimEvidenceVerifier>();

        IReadOnlyList<RagSource> sources =
        [
            new(
            "S1",
            100,
            1,
            "RAG Demonstration Security Status",
            "The current RAG demonstration application does not " +
            "implement document-level authorization. Retrieved " +
            "documents are not filtered according to the " +
            "requesting user's permissions.",
            0.25)
        ];

        const string claim = "The RAG demonstration application currently enforces document-level authorization.";

        // Act
        var result = await verifier.VerifyAsync(claim, sources);

        // Report
        Console.WriteLine($"Outcome: {result.Outcome}");
        Console.WriteLine($"Explanation: {result.Explanation}");

        // Assert
        Assert.AreEqual(ClaimSupportOutcome.Contradicted, result.Outcome);
    }
}