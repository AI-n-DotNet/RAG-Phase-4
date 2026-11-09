using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;

namespace AInDotNet.RagDemo.Tests.Evaluation;

// Console.Out is process-wide; do not redirect it alongside parallel tests.
[TestClass]
[TestCategory("Deterministic")]
[DoNotParallelize]
public sealed class EvaluationDiagnosticsTests
{
    [TestMethod]
    public async Task DefaultDiagnostics_DoNotWriteToConsole()
    {
        const string claim = "Enterprise RAG requires authorization.";
        const string answer = "Enterprise RAG requires authorization [S1].";
        const string claimJson = """["Enterprise RAG requires authorization."]""";
        var source = new RagSource("S1", 1, 1, "Requirements", claim, 0.1);
        var run = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-SILENT-DIAGNOSTICS",
            Question = "What does enterprise RAG require?",
            Answer = answer,
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        TextWriter originalOutput = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);

            Assert.AreEqual(1, CitedPassageExtractor.Extract(answer).Count);

            var classifier = new LlmClaimClassifier(new FakeChatClient("Factual"));
            Assert.AreEqual(ClaimType.Factual, await classifier.ClassifyAsync(claim));

            var entailment = new LlmClaimEntailmentVerifier(new FakeChatClient(
                """
                {
                    "sourceAttributionRequired": false,
                    "passageAttribution": null,
                    "claimAttribution": null,
                    "supportingSegment": "Enterprise RAG requires authorization [S1].",
                    "outcome": "Entailed",
                    "explanation": "The segment states the claim."
                }
                """));
            Assert.AreEqual(ClaimEntailmentOutcome.Entailed,
                await entailment.VerifyAsync(claim, answer, answer));

            var evidence = new LlmClaimEvidenceVerifier(new FakeChatClient(
                """
                {
                    "outcome": "Supported",
                    "supportingSourceLabels": ["S1"],
                    "explanation": "Source S1 states the requirement."
                }
                """));
            var faithfulness = new FaithfulnessEvaluator(
                new LlmClaimExtractor(new FakeChatClient(claimJson)),
                new LlmClaimNormalizer(new FakeChatClient(claimJson)),
                evidence);
            Assert.AreEqual(1.0, (await faithfulness.EvaluateAsync(run)).FaithfulnessScore);

            var citations = new CitationVerifier(
                new FakeCitedClaimExtractor(
                [
                    new CitedClaim
                    {
                        Claim = claim,
                        OriginalText = answer,
                        CitationLabels = ["S1"]
                    }
                ]),
                new CitedClaimSemanticValidator(
                    new LlmClaimSpanVerifier(new FakeChatClient("UNSUPPORTED"))),
                evidence,
                classifier);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => citations.VerifyAsync(run));
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        Assert.AreEqual(string.Empty, output.ToString());
    }

    [TestMethod]
    public void PassageDiagnostics_WhenEnabled_UseProvidedCallback()
    {
        const string answer = "Enterprise RAG requires authorization [S1].";
        var messages = new List<string>();

        var passages = CitedPassageExtractor.Extract(answer, messages.Add);

        Assert.AreEqual(1, passages.Count);
        Assert.AreEqual(answer, passages[0].OriginalText);
        CollectionAssert.AreEqual(
            new[] { $"PASSAGE 1: {answer}", "CITATIONS: S1" },
            messages);
    }

    [TestMethod]
    public async Task ClassifierDiagnostics_WhenEnabled_UseProvidedCallback()
    {
        const string claim = "Enterprise RAG requires authorization.";
        var messages = new List<string>();
        var classifier = new LlmClaimClassifier(
            new FakeChatClient("Factual"), messages.Add);

        ClaimType result = await classifier.ClassifyAsync(claim);

        Assert.AreEqual(ClaimType.Factual, result);
        CollectionAssert.AreEqual(
            new[] { $"CLAIM TYPE: Factual | {claim}" }, messages);
    }

    [TestMethod]
    public async Task PassageBasedExtractor_ForwardsDiagnosticCallback()
    {
        const string answer = "Enterprise RAG requires authorization [S1].";
        var messages = new List<string>();
        var extractor = new PassageBasedCitedClaimExtractor(
            new LlmPassageClaimExtractor(new FakeChatClient(
                """[{"claim":"Enterprise RAG requires authorization.","passageId":1}]""")),
            messages.Add);

        var claims = await extractor.ExtractAsync(answer);

        Assert.AreEqual(1, claims.Count);
        CollectionAssert.AreEqual(new[] { "S1" }, claims[0].CitationLabels.ToArray());
        CollectionAssert.AreEqual(
            new[] { $"PASSAGE 1: {answer}", "CITATIONS: S1" },
            messages);
    }
}
