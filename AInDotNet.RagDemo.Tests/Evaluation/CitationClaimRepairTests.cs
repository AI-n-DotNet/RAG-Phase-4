using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class CitationClaimRepairTests
{
    [TestMethod]
    public async Task VerifyAsync_ValidClaims_DoesNotRequestRepair()
    {
        var spans = new SequenceSpanVerifier(true);
        var repairer = new RecordingRepairer([]);
        var verifier = CreateVerifier([Claim("Valid assertion.")], spans, repairer);

        var result = await verifier.VerifyAsync(Run());

        Assert.AreEqual(0, repairer.Calls);
        Assert.AreEqual(1, spans.Calls.Count);
        Assert.AreEqual(1.0, result.CitationAccuracy);
    }

    [TestMethod]
    public async Task VerifyAsync_RejectedClaim_RepairsOnceAndPreservesMetadata()
    {
        var valid = Claim("Valid assertion.");
        var rejected = Claim("Authorization is required.");
        var spans = new SequenceSpanVerifier(true, false, true);
        var repairer = new RecordingRepairer(["They indicate that authorization is required."]);
        var diagnostics = new List<string>();
        var verifier = CreateVerifier([valid, rejected], spans, repairer, diagnostics.Add);

        var result = await verifier.VerifyAsync(Run());

        Assert.AreEqual(1, repairer.Calls);
        Assert.AreSame(rejected, repairer.ReceivedClaims.Single());
        Assert.AreEqual(3, spans.Calls.Count);
        Assert.AreEqual(rejected.OriginalText, spans.Calls[2].Original);
        Assert.AreEqual(rejected.ContextText, spans.Calls[2].Context);
        Assert.AreEqual(valid.Claim, result.Citations[0].Claim);
        Assert.AreEqual("They indicate that authorization is required.", result.Citations[1].Claim);
        Assert.AreEqual("S1", result.Citations[1].SourceLabel);
        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(2, result.TotalFactualClaims);
        Assert.AreEqual(1.0, result.CitationAccuracy);
        Assert.IsTrue(diagnostics.Any(message => message.Contains(rejected.Claim)));
        Assert.IsTrue(diagnostics.Any(message => message.StartsWith("REPAIRED CLAIM:")));
    }

    [TestMethod]
    public async Task VerifyAsync_RepairStillInvalid_FailsWithoutAnotherRepair()
    {
        var spans = new SequenceSpanVerifier(false, false);
        var repairer = new RecordingRepairer(["Still invalid."]);
        var verifier = CreateVerifier([Claim("Invalid.")], spans, repairer);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => verifier.VerifyAsync(Run()));

        StringAssert.StartsWith(exception.Message,
            "Cited claim remains invalid after one repair attempt.");
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(2, spans.Calls.Count);
    }

    [TestMethod]
    public async Task VerifyAsync_MultipleRejectedClaims_UsesOneBatchRepair()
    {
        var spans = new SequenceSpanVerifier(false, false, true, true);
        var repairer = new RecordingRepairer(["They indicate first invalid.", "They indicate second invalid."]);
        var verifier = CreateVerifier(
            [Claim("First invalid."), Claim("Second invalid.")], spans, repairer);

        var result = await verifier.VerifyAsync(Run());

        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(2, repairer.ReceivedClaims.Count);
        Assert.AreEqual(4, spans.Calls.Count);
        CollectionAssert.AreEqual(
            new[] { "They indicate first invalid.", "They indicate second invalid." },
            result.Citations.Select(citation => citation.Claim).ToArray());
    }

    [TestMethod]
    public async Task VerifyAsync_RepairDropsClaim_RejectsResponse()
    {
        var spans = new SequenceSpanVerifier(false);
        var repairer = new RecordingRepairer([]);
        var verifier = CreateVerifier([Claim("Invalid.")], spans, repairer);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => verifier.VerifyAsync(Run()));

        Assert.AreEqual(
            "Claim repair must return exactly one replacement per rejected claim.",
            exception.Message);
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(1, spans.Calls.Count);
    }

    [TestMethod]
    public async Task VerifyAsync_Cancelled_DoesNotRequestRepair()
    {
        var spans = new SequenceSpanVerifier(false);
        var repairer = new RecordingRepairer(["Replacement."]);
        var verifier = CreateVerifier([Claim("Invalid.")], spans, repairer);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => verifier.VerifyAsync(Run(), cancellation.Token));

        Assert.AreEqual(0, repairer.Calls);
        Assert.AreEqual(0, spans.Calls.Count);
    }

    [TestMethod]
    public async Task VerifyAsync_UnchangedRepair_FailsWithoutAnotherValidation()
    {
        const string text = "It is not stated that OAuth 2.0 is implemented in your application.";
        var rejected = new CitedClaim
        {
            Claim = text,
            OriginalText = "Another source explains OAuth 2.0 as a framework for delegated authorization but does not state that it is implemented in your application [S5].",
            CitationLabels = ["S5"]
        };
        var spans = new SequenceSpanVerifier(false);
        var repairer = new RecordingRepairer([text]);
        var verifier = CreateVerifier([rejected], spans, repairer);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => verifier.VerifyAsync(Run()));

        Assert.AreEqual(
            "Claim repair returned the rejected claim unchanged; no further attempt is allowed.",
            exception.Message);
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(1, spans.Calls.Count);
    }

    [TestMethod]
    public async Task VerifyAsync_WhitespaceAndCitationOnlyRepair_FailsWithoutAnotherValidation()
    {
        var spans = new SequenceSpanVerifier(false);
        var repairer = new RecordingRepairer(
            ["They  indicate\nthat authorization is required [S1]."]);
        var verifier = CreateVerifier(
            [Claim("They indicate that authorization is required.")], spans, repairer);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => verifier.VerifyAsync(Run()));

        Assert.AreEqual(
            "Claim repair returned the rejected claim unchanged; no further attempt is allowed.",
            exception.Message);
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(1, spans.Calls.Count);
    }

    [TestMethod]
    public async Task VerifyAsync_RepairDropsRecordedPurposeClause_RejectsBeforeRevalidation()
    {
        const string original =
            "Enterprise RAG systems must enforce authorization controls, among other security measures, to ensure that access to documents is properly controlled and that documents are not accessible solely based on semantic relevance.";
        var spans = new SequenceSpanVerifier(false);
        var repairer = new RecordingRepairer(
            ["The retrieved sources state that enterprise RAG systems must enforce authorization controls, among other security measures."]);
        var verifier = CreateVerifier([Claim(original)], spans, repairer);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => verifier.VerifyAsync(Run()));

        StringAssert.StartsWith(exception.Message,
            "Claim repair removed or reordered original claim content;");
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(1, spans.Calls.Count);
    }

    [TestMethod]
    public async Task VerifyAsync_RepairPreservesEntirePurposeClause_AcceptsAfterValidation()
    {
        const string original =
            "Enterprise RAG systems must enforce authorization controls to ensure access is controlled and documents are not accessible solely based on semantic relevance.";
        const string repaired =
            "The retrieved sources state that enterprise RAG systems must enforce authorization controls to ensure access is controlled and documents are not accessible solely based on semantic relevance.";
        var spans = new SequenceSpanVerifier(false, true);
        var repairer = new RecordingRepairer([repaired]);
        var verifier = CreateVerifier([Claim(original)], spans, repairer);

        var result = await verifier.VerifyAsync(Run());

        Assert.AreEqual(repaired, result.Citations.Single().Claim);
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(2, spans.Calls.Count);
    }

    [TestMethod]
    [DataRow("Authorization is not confirmed.", "The sources state that authorization is confirmed.")]
    [DataRow("Authorization is required if access is restricted.", "The sources state that authorization is required.")]
    [DataRow("Authorization and auditing are required.", "The sources state that authorization is required.")]
    public async Task VerifyAsync_RepairDropsClaimContent_Rejects(
        string original, string repaired)
    {
        var spans = new SequenceSpanVerifier(false);
        var repairer = new RecordingRepairer([repaired]);
        var verifier = CreateVerifier([Claim(original)], spans, repairer);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => verifier.VerifyAsync(Run()));

        StringAssert.StartsWith(exception.Message,
            "Claim repair removed or reordered original claim content;");
        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(1, spans.Calls.Count);
    }

    [TestMethod]
    [DataRow("It is mentioned that authorization is required to ensure access is controlled.",
        "They mention that authorization is required to ensure access is controlled.", true)]
    [DataRow("It is not confirmed that authorization controls are implemented.",
        "They do not confirm that authorization controls are implemented.", true)]
    [DataRow("It is mentioned that authorization is required to ensure access is controlled.",
        "They mention that authorization is required.", false)]
    [DataRow("It is not confirmed that authorization controls are implemented.",
        "They confirm that authorization controls are implemented.", false)]
    [DataRow("It is mentioned that authorization and auditing are required.",
        "They mention that authorization is required.", false)]
    public async Task VerifyAsync_ReplaceImpersonalFrame_PreservesBodyAndReportingNegation(
        string original, string repaired, bool expected)
    {
        var spans = new SequenceSpanVerifier(false, true);
        var repairer = new RecordingRepairer([repaired]);
        var verifier = CreateVerifier([Claim(original)], spans, repairer);

        if (expected)
        {
            var result = await verifier.VerifyAsync(Run());
            Assert.AreEqual(repaired, result.Citations.Single().Claim);
        }
        else
        {
            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => verifier.VerifyAsync(Run()));
            StringAssert.StartsWith(exception.Message,
                "Claim repair removed or reordered original claim content;");
        }

        Assert.AreEqual(1, repairer.Calls);
        Assert.AreEqual(expected ? 2 : 1, spans.Calls.Count);
    }

    private static CitedClaim Claim(string text) => new()
    {
        Claim = text,
        OriginalText = "They indicate that authorization is required [S1].",
        ContextText = "They indicate that authorization is required [S1].",
        CitationLabels = ["S1"]
    };

    private static RagEvaluationRun Run()
    {
        var source = new RagSource("S1", 1, 1, "Requirements", "Authorization is required.", 0.1);
        return new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-BOUNDED-REPAIR",
            Question = "What is required?",
            Answer = "They indicate that authorization is required [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };
    }

    private static CitationVerifier CreateVerifier(
        IReadOnlyList<CitedClaim> claims, IClaimSpanVerifier spans,
        ICitedClaimRepairer repairer, Action<string>? diagnostics = null) =>
        new(new FakeCitedClaimExtractor(claims),
            new CitedClaimSemanticValidator(spans),
            new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported),
            new FakeClaimClassifier(),
            diagnostics, repairer);

    private sealed class SequenceSpanVerifier(params bool[] outcomes) : IClaimSpanVerifier
    {
        private readonly Queue<bool> _outcomes = new(outcomes);
        public List<(string Claim, string Original, string Context)> Calls { get; } = [];

        public Task<bool> VerifyAsync(
            string claim, string originalText, CancellationToken cancellationToken = default) =>
            VerifyAsync(claim, originalText, originalText, cancellationToken);

        public Task<bool> VerifyAsync(
            string claim, string originalText, string contextText,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((claim, originalText, contextText));
            return Task.FromResult(_outcomes.Dequeue());
        }
    }

    private sealed class RecordingRepairer(IReadOnlyList<string> replacements) : ICitedClaimRepairer
    {
        public int Calls { get; private set; }
        public IReadOnlyList<CitedClaim> ReceivedClaims { get; private set; } = [];

        public Task<IReadOnlyList<string>> RepairAsync(
            IReadOnlyList<CitedClaim> rejectedClaims, CancellationToken cancellationToken = default)
        {
            Calls++;
            ReceivedClaims = rejectedClaims;
            return Task.FromResult(replacements);
        }
    }
}
