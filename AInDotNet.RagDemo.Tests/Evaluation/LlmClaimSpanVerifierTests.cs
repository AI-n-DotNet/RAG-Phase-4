using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Application.Retrieval;
using AInDotNet.RagDemo.Data.Models;
using AInDotNet.RagDemo.Tests.Integration;
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
public sealed class LlmClaimSpanVerifierTests
{
    private readonly IChatClient _chatClient;


    public LlmClaimSpanVerifierTests()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets<LlmClaimSpanVerifierTests>();
        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");
        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";
        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        _chatClient = host.Services.GetRequiredService<IChatClient>();
    }


    [TestMethod]
    public async Task VerifyAsync_SupportedClaim_ReturnsTrue()
    {
        // Arrange
        var verifier = new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"));

        const string claim = "Enterprise RAG requires authorization.";

        const string originalText = "Enterprise RAG requires authorization [S1].";

        // Act
        bool result = await verifier.VerifyAsync(claim, originalText);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task VerifyAsync_UnsupportedClaim_ReturnsFalse()
    {
        // Arrange
        var verifier = new LlmClaimSpanVerifier(new FakeChatClient("UNSUPPORTED"));

        const string claim = "Enterprise RAG requires authorization.";

        const string originalText = "OAuth 2.0 provides delegated authorization [S5].";

        // Act
        bool result = await verifier.VerifyAsync(claim, originalText);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_InvalidResponse_ThrowsException()
    {
        // Arrange
        var verifier = new LlmClaimSpanVerifier(new FakeChatClient("MAYBE"));

        const string claim = "Enterprise RAG requires authorization.";

        const string originalText = "Enterprise RAG requires authorization [S1].";

        // Act and Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => verifier.VerifyAsync(claim, originalText));

        Assert.AreEqual("Unexpected claim-span verification response: MAYBE", exception.Message);
    }

    [TestMethod]
    public async Task VerifyAsync_EvidenceAbsence_DoesNotEstablishImplementationAbsence()
    {
        const string originalText = "The retrieved passages do not specify which authorization controls are implemented.";

        const string claim = "The application does not implement authorization controls.";

        var verifier = new LlmClaimSpanVerifier(_chatClient);

        bool supported = await verifier.VerifyAsync(claim, originalText);

        Assert.IsFalse(supported);
    }

    [TestMethod]
    public async Task VerifyAsync_UnconfirmedImplementation_DoesNotEstablishNonImplementation()
    {

        const string originalText = "The retrieved sources do not establish whether " + "OAuth 2.0 is implemented in the RAG demonstration application. " + "OAuth 2.0 may or may not be implemented.";

        const string claim = "OAuth 2.0 is not implemented in the RAG demo application.";

        var verifier = new LlmClaimSpanVerifier(_chatClient);

        bool supported = await verifier.VerifyAsync(claim, originalText);

        Assert.IsFalse(supported);
    }

    [TestMethod]
    public async Task VerifyAsync_AmbiguousApplicationStatus_RejectsNonImplementation()
    {

        const string originalText = "OAuth 2.0 is noted as a framework for delegated " + "authorization generally but not specifically applied " + "to your RAG demo app [S5].";

        const string claim = "OAuth 2.0 is not implemented in the RAG demo application.";

        var verifier = new LlmClaimSpanVerifier(_chatClient);

        bool supported = await verifier.VerifyAsync(claim, originalText);

        Assert.IsFalse(supported);
    }

    [TestMethod]
    public async Task VerifyAsync_AmbiguousOAuthImplementation_ReturnsNotEstablished()
    {
        const string originalText = "OAuth 2.0 is noted as a framework for delegated " + "authorization generally but not specifically applied " + "to your RAG demo app [S5].";

        const string claim = "OAuth 2.0 is not implemented in the RAG demo application.";

        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText);

        Console.WriteLine($"ENTAILMENT OUTCOME: {outcome}");

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ExplicitAuthorizationRequirement_ReturnsEntailed()
    {
        const string originalText = "Enterprise RAG systems must enforce authorization " + "to prevent unauthorized access to documents [S1].";

        const string claim = "Enterprise RAG systems must enforce authorization.";

        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText);

        Console.WriteLine($"ENTAILMENT OUTCOME: {outcome}");

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ExplicitAuthorizationRequirement_ReturnsContradicted()
    {
        const string originalText = "Enterprise RAG systems must enforce authorization " + "to prevent unauthorized access to documents [S1].";

        const string claim = "Enterprise RAG systems do not need to enforce authorization.";

        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText);

        Console.WriteLine($"ENTAILMENT OUTCOME: {outcome}");

        Assert.AreEqual(ClaimEntailmentOutcome.Contradicted, outcome);
    }

    [TestMethod]
    public async Task IsSupportedAsync_AmbiguousOAuthImplementation_ReturnsFalse()
    {
        const string originalText = "OAuth 2.0 is noted as a framework for delegated " + "authorization generally but not specifically applied " + "to your RAG demo app [S5].";

        const string claim = "OAuth 2.0 is not implemented in the RAG demo application.";

        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        bool supported = await verifier.IsSupportedAsync(claim, originalText);

        Assert.IsFalse(supported);
    }

    [TestMethod]
    public async Task VerifyAsync_EntailmentAdapter_AmbiguousOAuth_ReturnsFalse()
    {
        const string originalText = "OAuth 2.0 is noted as a framework for delegated " + "authorization generally but not specifically applied " + "to your RAG demo app [S5].";

        const string claim = "OAuth 2.0 is not implemented in the RAG demo application.";

        IClaimEntailmentVerifier entailmentVerifier = new LlmClaimEntailmentVerifier(_chatClient);

        IClaimSpanVerifier verifier = new ClaimEntailmentSpanVerifierAdapter(entailmentVerifier);

        bool supported = await verifier.VerifyAsync(claim, originalText);

        Assert.IsFalse(supported);
    }

    [TestMethod]
    public async Task VerifyAsync_ImplicitSubjectWithFullContext_ReturnsTrue()
    {
        // Arrange
        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        string originalText = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        string claim = "The sources do not confirm implementation of authorization controls.";

        // Act
        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText);

        // Assert
        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_UnrelatedClaimFromOtherClause_ReturnsNotEstablished()
    {
        // Arrange

        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        string originalText = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        string claim = "The RAG demonstration application implements authorization controls.";

        // Act
        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText);

        // Assert
        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ClaimFromDifferentSegment_ReturnsNotEstablished()
    {
        // Arrange

        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        string contextText = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        string originalText = "but do not confirm their implementation.";

        string claim = "The sources recommend authorization controls.";

        // Act
        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText, contextText);

        // Assert
        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ImplicitSubjectInAssignedSegment_ReturnsEntailed()
    {
        // Arrange
        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        string contextText = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        string originalText = "but do not confirm their implementation.";

        string claim = "The sources do not confirm implementation of authorization controls.";

        // Act
        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, originalText, contextText);

        // Assert
        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task VerifyAsync_AttributedOAuthClaim_PreservesSourceAttribution()
    {
        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        const string passage = "While the sources mention that enterprise RAG systems must " + "enforce authorization and that OAuth 2.0 provides a framework " + "for delegated authorization, they do not confirm that these " + "controls are implemented in the demonstration application " + "itself [S1, S5].";

        const string claim = "The sources mention that OAuth 2.0 provides a framework " + "for delegated authorization.";

        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, passage, passage);

        Console.WriteLine($"Entailment outcome: {outcome}");

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task VerifyAsync_UnattributedOAuthClaim_ReportsOutcome()
    {
        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        const string passage = "While the sources mention that enterprise RAG systems must " + "enforce authorization and that OAuth 2.0 provides a framework " + "for delegated authorization, they do not confirm that these " + "controls are implemented in the demonstration application " + "itself [S1, S5].";

        const string claim = "OAuth 2.0 provides a framework for delegated authorization.";

        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, passage, passage);

        Console.WriteLine($"Entailment outcome: {outcome}");

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task VerifyAsync_RecommendationInConcessiveClause_ReturnsEntailed()
    {
        var verifier = new LlmClaimEntailmentVerifier(_chatClient);

        const string passage = "While it is recommended that enterprise RAG systems enforce " + "authorization among other controls, the sources do not confirm " + "any actual implementation of authorization controls in your " + "application [S1].";

        const string claim = "It is recommended that enterprise RAG systems enforce " + "authorization among other controls.";

        ClaimEntailmentOutcome outcome = await verifier.VerifyAsync(claim, passage, passage);

        Console.WriteLine($"Entailment outcome: {outcome}");

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }
}
