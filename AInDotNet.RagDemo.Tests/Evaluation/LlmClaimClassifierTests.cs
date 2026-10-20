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
public sealed class LlmClaimClassifierTests : IDisposable
{
    private readonly IHost host;
    private readonly IChatClient _chatClient;

    public LlmClaimClassifierTests()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimExtractionIntegrationTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddTransient<IClaimExtractor, LlmClaimExtractor>();
        builder.Services.AddTransient<IClaimNormalizer, LlmClaimNormalizer>();

        host = builder.Build();
        _chatClient = host.Services.GetRequiredService<IChatClient>();
    }

    public void Dispose()
    {
        host.Dispose();
    }


    [TestMethod]
    public async Task ClassifyAsync_MissingRetrievedEvidence_ReturnsCollectionEvidence()
    {
        var classifier = new LlmClaimClassifier(_chatClient);

        const string claim = "The retrieved passages do not specify which " + "authorization controls are implemented.";

        ClaimType result = await classifier.ClassifyAsync(claim);

        Assert.AreEqual(ClaimType.CollectionEvidence, result);
    }

    [TestMethod]
    public async Task ClassifyAsync_MissingImplementation_ReturnsFactual()
    {
        var classifier = new LlmClaimClassifier(_chatClient);

        const string claim = "The RAG demonstration application does not " + "implement authorization controls.";

        ClaimType result = await classifier.ClassifyAsync(claim);

        Assert.AreEqual(ClaimType.Factual, result);
    }

    [TestMethod]
    public async Task ClassifyAsync_NoIndicationOfImplementation_ReturnsCollectionEvidence()
    {
        const string claim = "There is no indication that authorization controls " + "are actively implemented in your demonstration application.";

        var classifier = new LlmClaimClassifier(_chatClient);

        ClaimType result = await classifier.ClassifyAsync(claim);

        Console.WriteLine($"CLAIM TYPE: {result}");

        Assert.AreEqual(ClaimType.CollectionEvidence, result);
    }

    [TestMethod]
    public async Task ClassifyAsync_ExplicitRetrievedEvidenceAbsence_ReturnsCollectionEvidence()
    {
        const string claim = "The retrieved sources provide no indication that " + "authorization controls are actively implemented " + "in the demonstration application.";

        var classifier = new LlmClaimClassifier(_chatClient);

        ClaimType result = await classifier.ClassifyAsync(claim);

        Console.WriteLine($"CLAIM TYPE: {result}");

        Assert.AreEqual(ClaimType.CollectionEvidence, result);
    }
}