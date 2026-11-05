using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public sealed class LlmClaimEvidenceVerifierTests
{
    [TestMethod]
    public async Task VerifyAsync_CollectionWideAbsence_ContradictedByOneSource()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimExtractionIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        using var host = builder.Build();
        var scope = host.Services.CreateScope();

        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();


        const string claim = "None of the retrieved passages specify which " + "authorization controls are implemented.";

        RagSource[] sources =
        [
            new(
            "S1",
            1,
            1,
            "RAG Architecture",
            "Enterprise RAG systems should enforce authorization.",
            0.1),

        new(
            "S2",
            2,
            2,
            "RAG Implementation",
            "The RAG demonstration application currently implements " +
            "role-based access control (RBAC) for document retrieval.",
            0.2)
        ];

        var verifier = new LlmClaimEvidenceVerifier(chatClient);

        // Act
        ClaimEvaluationResult result = await verifier.VerifyAsync(claim, sources);

        // Assert
        Assert.AreEqual(ClaimSupportOutcome.Contradicted, result.Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_CollectionWideAbsence_SingleSourceDoesNotEstablishCollection()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimExtractionIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        using var host = builder.Build();
        var scope = host.Services.CreateScope();

        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();

        const string claim = "None of the retrieved passages specify which " + "authorization controls are implemented.";

        RagSource[] sources =
        [
            new(
            "S1",
            1,
            1,
            "RAG Architecture",
            "Enterprise RAG systems should enforce authorization.",
            0.1)
        ];

        var verifier = new LlmClaimEvidenceVerifier(chatClient);

        //ClaimEvaluationResult result = await verifier.VerifyAsync(claim, sources);
        ClaimEvaluationResult result = await verifier.VerifyAsync(claim, sources, EvidenceScope.IndividualSource);

        Assert.AreEqual(ClaimSupportOutcome.InsufficientEvidence, result.Outcome);
    }
}
