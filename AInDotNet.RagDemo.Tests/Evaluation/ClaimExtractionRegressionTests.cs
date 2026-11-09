using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
public sealed class ClaimExtractionRegressionTests
{
    // Captured from the failing live run. Keep this input fixed.
    private const string Answer =
        "The retrieved sources do not provide information about the specific authorization " +
        "controls currently implemented in your RAG demonstration application. " +
        "While they indicate that enterprise RAG systems must enforce authorization controls " +
        "among other security measures [S1], there is no explicit evidence describing the " +
        "implementation status of such controls in your demonstration. " +
        "Therefore, it cannot be determined from the provided sources whether authorization " +
        "controls are currently implemented in your RAG demonstration application.";

    private const string BadClaim =
        "Enterprise RAG systems must enforce authorization controls among other security measures [S1].";

    [TestCategory("Deterministic")]
    [TestMethod]
    public async Task RecordedDroppedAttribution_IsRejected()
    {
        var passages = CitedPassageExtractor.Extract(Answer);
        var assigned = passages.Single(p => p.OriginalText.StartsWith("While they indicate"));
        string extractionResponse = JsonSerializer.Serialize(new[]
        {
            new { claim = BadClaim, passageId = assigned.PassageId }
        });
        var extractor = new PassageBasedCitedClaimExtractor(
            new LlmPassageClaimExtractor(new FakeChatClient(extractionResponse)));

        var claims = await extractor.ExtractAsync(Answer);

        Assert.AreEqual(1, claims.Count);
        Assert.AreEqual(BadClaim.Replace(" [S1]", string.Empty), claims[0].Claim);
        Assert.AreEqual(assigned.OriginalText, claims[0].OriginalText);
        CollectionAssert.AreEqual(new[] { "S1" }, claims[0].CitationLabels.ToArray());

        // Even a mistaken positive judge verdict must not accept dropped attribution.
        var judge = new LlmClaimEntailmentVerifier(new FakeChatClient(
            """
            {
                "sourceAttributionRequired": true,
                "supportingSegment": "they indicate that enterprise RAG systems must enforce authorization controls among other security measures [S1]",
                "passageAttribution": "they indicate",
                "claimAttribution": null,
                "outcome": "Entailed",
                "explanation": "Simulated mistaken positive verdict."
            }
            """));
        var validator = new CitedClaimSemanticValidator(
            new ClaimEntailmentSpanVerifierAdapter(judge));

        Assert.IsFalse(await validator.ValidateAsync(claims[0]),
            "The recorded extraction dropped the original source attribution.");
    }

    [TestCategory("ExternalDependency")]
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("LiveEvaluation")]
    public async Task FixedAnswer_LiveExtraction_PreservesClaimProvenance()
    {
        using IChatClient client = CreateLiveClient();

        var passages = CitedPassageExtractor.Extract(Answer, Console.WriteLine);
        var extractor = new LlmPassageClaimExtractor(client);

        var extracted = await extractor.ExtractAsync(passages);

        Assert.IsTrue(extracted.Count > 0, "Extraction must not omit every assertion.");

        // Print all claims before validation so a failure preserves the whole extraction.
        foreach (var claim in extracted)
            Console.WriteLine($"PASSAGE {claim.PassageId} CLAIM: {claim.Claim}");

        var judge = new LlmClaimEntailmentVerifier(client, Console.WriteLine);
        var validator = new CitedClaimSemanticValidator(
            new ClaimEntailmentSpanVerifierAdapter(judge));

        foreach (var claim in extracted)
        {
            CitedClaim resolved = PassageClaimResolver.Resolve(claim, passages);

            Assert.AreEqual(0, CitationReferenceExtractor.Extract(claim.Claim).Count,
                $"Citation labels belong in source metadata, not claim text: {claim.Claim}");

            Assert.IsTrue(await validator.ValidateAsync(resolved),
                $"Extraction must preserve the assigned passage's attribution and scope: {claim.Claim}");
        }

        var requirementPassage = passages.Single(p =>
            p.OriginalText.StartsWith("While they indicate"));
        Assert.IsTrue(extracted.Count(c => c.PassageId == requirementPassage.PassageId) >= 2,
            "The requirement passage contains both an attributed requirement and an evidence limitation.");
    }

    [TestCategory("ExternalDependency")]
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("LiveEvaluation")]
    public async Task RecordedDroppedAttribution_LiveRepair_PreservesSourceOwnership()
    {
        using IChatClient client = CreateLiveClient();
        var passages = CitedPassageExtractor.Extract(Answer);
        var assigned = passages.Single(p => p.OriginalText.StartsWith("While they indicate"));
        var rejected = new CitedClaim
        {
            Claim = CitationReferenceExtractor.RemoveMarkers(BadClaim),
            OriginalText = assigned.OriginalText,
            ContextText = assigned.ContextText,
            CitationLabels = assigned.CitationLabels
        };
        var messages = new List<string>();
        void WriteDiagnostic(string message)
        {
            messages.Add(message);
            Console.WriteLine(message);
        }

        // Replay the known rejection so this test always exercises repair.
        // Only the repaired text is judged live; extraction is fixed.
        var liveVerifier = new ClaimEntailmentSpanVerifierAdapter(
            new LlmClaimEntailmentVerifier(client, WriteDiagnostic));
        var spans = new RecordedRejectionThenLiveVerifier(liveVerifier);
        var verifier = new CitationVerifier(
            new FakeCitedClaimExtractor([rejected]),
            new CitedClaimSemanticValidator(spans),
            new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported),
            new FakeClaimClassifier(),
            WriteDiagnostic,
            new LlmCitedClaimRepairer(client));

        // Evidence support and classification are faked: this test assesses
        // semantic repair and source ownership, not retrieval or evidence quality.
        var source = new RagSource(
            "S1", 1, 1, "Requirements",
            "Enterprise RAG systems must enforce authorization.", 0.1);
        var run = new RagEvaluationRun
        {
            EvaluationCaseId = "RECORDED-ATTRIBUTION-REPAIR",
            Question = "Which authorization controls are implemented?",
            Answer = Answer,
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        var result = await verifier.VerifyAsync(run);

        Assert.AreEqual(2, spans.Calls, "Replay one rejection, then validate one repair live.");
        Assert.AreEqual(1, messages.Count(message =>
            message.StartsWith("CLAIM REPAIR ATTEMPT: 1;")));
        Assert.AreEqual(1, result.TotalClaims);
        Assert.AreEqual("S1", result.Citations.Single().SourceLabel);
        Assert.AreEqual(0, CitationReferenceExtractor.Extract(result.Citations[0].Claim).Count);
        Assert.AreEqual(assigned.OriginalText, rejected.OriginalText);
        CollectionAssert.AreEqual(new[] { "S1" }, rejected.CitationLabels.ToArray());
    }

    private static IChatClient CreateLiveClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ClaimExtractionRegressionTests>()
            .Build();
        string apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key is missing.");
        string model = configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";
        return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
    }

    private sealed class RecordedRejectionThenLiveVerifier(
        IClaimSpanVerifier liveVerifier) : IClaimSpanVerifier
    {
        public int Calls { get; private set; }

        public Task<bool> VerifyAsync(
            string claim, string originalText, CancellationToken cancellationToken = default) =>
            VerifyAsync(claim, originalText, originalText, cancellationToken);

        public Task<bool> VerifyAsync(
            string claim, string originalText, string contextText,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Calls == 1
                ? Task.FromResult(false)
                : liveVerifier.VerifyAsync(claim, originalText, contextText, cancellationToken);
        }
    }
}
