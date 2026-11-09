using AInDotNet.RagDemo.Evaluation;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class LlmClaimEntailmentVerifierTests
{
    private const string Passage =
        "The sources mention that OAuth 2.0 provides delegated authorization.";
    private const string UnattributedClaim =
        "OAuth 2.0 provides delegated authorization.";

    [TestMethod]
    public async Task VerifyAsync_EntailedWithMissingClaimAttribution_ReturnsNotEstablished()
    {
        var verifier = CreateVerifier("The sources mention", null);

        var outcome = await verifier.VerifyAsync(UnattributedClaim, Passage, Passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_EntailedWithInventedClaimAttribution_ReturnsNotEstablished()
    {
        var verifier = CreateVerifier("The sources mention", "The sources mention");

        var outcome = await verifier.VerifyAsync(UnattributedClaim, Passage, Passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_PreservedAttribution_ReturnsEntailed()
    {
        var verifier = CreateVerifier("The sources mention", "The sources mention");

        var outcome = await verifier.VerifyAsync(Passage, Passage, Passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_NoAttributionRequired_ReturnsEntailed()
    {
        var verifier = CreateVerifier(null, null, supportingSegment: UnattributedClaim);

        var outcome = await verifier.VerifyAsync(
            UnattributedClaim, UnattributedClaim, UnattributedClaim);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_AttributionPhraseAlreadyInClaim_ReturnsEntailed()
    {
        const string passage =
            "While it is recommended that enterprise RAG systems enforce " +
            "authorization among other controls, the sources do not confirm " +
            "any actual implementation of authorization controls in your application [S1].";
        const string claim =
            "It is recommended that enterprise RAG systems enforce " +
            "authorization among other controls.";
        var verifier = CreateVerifier(
            "it is recommended that enterprise RAG systems enforce authorization among other controls",
            null, supportingSegment: passage);

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_NotEstablishedWithNoncontiguousDiagnosticAttribution_PreservesVerdict()
    {
        const string passage =
            "While the sources mention that enterprise RAG systems must enforce authorization " +
            "and that OAuth 2.0 provides a framework for delegated authorization, " +
            "they do not confirm implementation.";
        var verifier = CreateVerifier(
            "the sources mention that OAuth 2.0 provides a framework for delegated authorization",
            null,
            ClaimEntailmentOutcome.NotEstablished);

        var outcome = await verifier.VerifyAsync(UnattributedClaim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_EntailedWithQuoteOnlyInContext_ReturnsNotEstablished()
    {
        const string context =
            "The sources recommend authorization controls [S1], but do not confirm their implementation.";
        const string assigned = "but do not confirm their implementation.";
        var verifier = CreateVerifier(
            "The sources recommend", "The sources recommend",
            supportingSegment: "The sources recommend authorization controls");

        var outcome = await verifier.VerifyAsync(
            "The sources recommend authorization controls.", assigned, context);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_AssignedAssertionWithResolvedSubject_ReturnsEntailed()
    {
        const string context =
            "The sources recommend authorization controls [S1], but do not confirm their implementation.";
        const string assigned = "but do not confirm their implementation.";
        var verifier = CreateVerifier(
            "The sources", "The sources", supportingSegment: assigned);

        var outcome = await verifier.VerifyAsync(
            "The sources do not confirm implementation of authorization controls.", assigned, context);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_EntailedWithoutSupportingQuote_ReturnsNotEstablished()
    {
        var verifier = CreateVerifier(
            "The sources mention", "The sources mention", supportingSegment: null);

        var outcome = await verifier.VerifyAsync(
            "The sources mention that delegated authorization is provided by OAuth 2.0.",
            Passage, Passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_CoordinatedClausePreservesSourceSubject_ReturnsEntailed()
    {
        const string passage =
            "The retrieved sources mention that enterprise RAG systems must enforce authorization, " +
            "but do not explicitly state which authorization controls are implemented in your RAG demonstration application.";
        const string supporting =
            "but do not explicitly state which authorization controls are implemented in your RAG demonstration application.";
        const string claim =
            "The retrieved sources do not explicitly state which authorization controls are implemented in your RAG demonstration application.";
        var verifier = CreateVerifier(
            "The retrieved sources mention", null, supportingSegment: supporting);

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_CoordinatedClauseChangesSourceSubject_ReturnsNotEstablished()
    {
        const string passage =
            "The retrieved sources mention authorization, but do not confirm implementation.";
        const string claim = "The auditors do not confirm implementation.";
        var verifier = CreateVerifier(
            "The retrieved sources mention", null,
            supportingSegment: "but do not confirm implementation.");

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ImpersonalAssertionIgnoresUnrelatedReportingPhrase_ReturnsEntailed()
    {
        const string passage =
            "OAuth 2.0 is mentioned as a framework for delegated authorization to protected resources, " +
            "but there is no indication that it is utilized in the current RAG demonstration application [S1][S5].";
        const string claim =
            "There is no indication that OAuth 2.0 is utilized in the current RAG demonstration application.";
        var verifier = CreateVerifier(
            "OAuth 2.0 is mentioned", null,
            supportingSegment: "there is no indication that it is utilized in the current RAG demonstration application",
            sourceAttributionRequired: false);

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_RequiredAttributionWithoutSourceCue_ReturnsNotEstablished()
    {
        var verifier = CreateVerifier(
            null, null, sourceAttributionRequired: true);

        var outcome = await verifier.VerifyAsync(UnattributedClaim, Passage, Passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_VerbatimCoordinatedClause_OverridesMistakenNegativeVerdict()
    {
        const string passage =
            "They indicate that enterprise RAG systems must enforce authorization among other controls, " +
            "but do not confirm the actual implementation of such controls in the demonstration.";
        const string supporting =
            "but do not confirm the actual implementation of such controls in the demonstration.";
        const string claim =
            "They do not confirm the actual implementation of such controls in the demonstration.";
        var verifier = CreateVerifier(
            "They indicate", null, ClaimEntailmentOutcome.NotEstablished,
            supportingSegment: supporting);

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ChangedCoordinatedPredicate_PreservesNegativeVerdict()
    {
        const string passage =
            "They indicate that authorization is required, but do not confirm implementation.";
        var verifier = CreateVerifier(
            "They indicate", null, ClaimEntailmentOutcome.NotEstablished,
            supportingSegment: "but do not confirm implementation.");

        var outcome = await verifier.VerifyAsync(
            "They confirm implementation.", passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_CoordinatedAssertionOnlyInContext_PreservesNegativeVerdict()
    {
        const string context =
            "They indicate that authorization is required, but do not confirm implementation.";
        const string assigned = "They indicate that authorization is required.";
        var verifier = CreateVerifier(
            "They indicate", null, ClaimEntailmentOutcome.NotEstablished,
            supportingSegment: "but do not confirm implementation.");

        var outcome = await verifier.VerifyAsync(
            "They do not confirm implementation.", assigned, context);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_AbbreviatedSupportingQuote_RestoresAssignedSpan()
    {
        const string passage =
            "They mention that enterprise RAG systems must enforce authorization " +
            "and that OAuth 2.0 provides a framework for delegated authorization, " +
            "but do not confirm implementation in the demonstration app [S1], [S5].";
        const string claim =
            "They mention that OAuth 2.0 provides a framework for delegated authorization.";
        var verifier = CreateVerifier(
            "They mention", "They mention",
            supportingSegment: "they mention that ... OAuth 2.0 provides a framework for delegated authorization");

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_AbbreviatedQuoteFoundOnlyInContext_ReturnsNotEstablished()
    {
        const string assigned = "They do not confirm implementation.";
        const string context =
            "They mention that authorization is required and that OAuth provides delegated authorization.";
        var verifier = CreateVerifier(
            "They mention", "They mention",
            supportingSegment: "They mention that ... OAuth provides delegated authorization");

        var outcome = await verifier.VerifyAsync(
            "They mention that OAuth provides delegated authorization.", assigned, context);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_AmbiguousAbbreviatedQuote_ReturnsNotEstablished()
    {
        const string passage =
            "They mention that authorization is required. " +
            "They mention that OAuth provides delegated authorization.";
        var verifier = CreateVerifier(
            "They mention", "They mention",
            supportingSegment: "They mention that ... OAuth provides delegated authorization");

        var outcome = await verifier.VerifyAsync(
            "They mention that OAuth provides delegated authorization.", passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_JoinedAttributionQuote_RestoresCoordinatedSourceSpan()
    {
        const string passage =
            "While the sources mention that enterprise RAG systems must enforce authorization " +
            "and that OAuth 2.0 provides a framework for delegated authorization, " +
            "they do not confirm implementation [S1, S5].";
        const string claim =
            "The sources mention that OAuth 2.0 provides a framework for delegated authorization.";
        var verifier = CreateVerifier(
            "the sources mention", "The sources mention",
            supportingSegment: "the sources mention that OAuth 2.0 provides a framework for delegated authorization");

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_JoinedQuoteAssertionOnlyInContext_ReturnsNotEstablished()
    {
        const string assigned = "The sources mention that authorization is required.";
        const string context =
            "The sources mention that authorization is required and that OAuth provides delegated authorization.";
        var verifier = CreateVerifier(
            "The sources mention", "The sources mention",
            supportingSegment: "The sources mention that OAuth provides delegated authorization");

        var outcome = await verifier.VerifyAsync(
            "The sources mention that OAuth provides delegated authorization.", assigned, context);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_RestoredJoinedQuoteDoesNotExcuseDroppedAttribution()
    {
        const string passage =
            "The sources mention that authorization is required and that OAuth provides delegated authorization.";
        var verifier = CreateVerifier(
            "The sources mention", null,
            supportingSegment: "The sources mention that OAuth provides delegated authorization");

        var outcome = await verifier.VerifyAsync(
            "OAuth provides delegated authorization.", passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_SharedSubjectQuote_RestoresUniqueOrderedSpan()
    {
        const string passage =
            "They indicate that authorization is required and mention OAuth 2.0 " +
            "as a framework for delegated authorization, but they do not confirm implementation.";
        const string claim =
            "They mention OAuth 2.0 as a framework for delegated authorization.";
        var verifier = CreateVerifier(
            "They ... mention OAuth 2.0 as a framework for delegated authorization",
            "They mention OAuth 2.0 as a framework for delegated authorization",
            supportingSegment: "They mention OAuth 2.0 as a framework for delegated authorization");

        var outcome = await verifier.VerifyAsync(claim, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_SharedSubjectRestoration_DoesNotExcuseDroppedAttribution()
    {
        const string passage =
            "They indicate that authorization is required and mention OAuth 2.0 " +
            "as a framework for delegated authorization, but they do not confirm implementation.";
        var verifier = CreateVerifier(
            "They ... mention OAuth 2.0 as a framework for delegated authorization",
            null,
            supportingSegment: "They mention OAuth 2.0 as a framework for delegated authorization");

        var outcome = await verifier.VerifyAsync(
            "OAuth 2.0 is mentioned as a framework for delegated authorization.", passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_CompleteAssignedSegmentCopy_DoesNotUseModelVerdict()
    {
        const string passage =
            "The retrieved sources do not provide information about implemented controls.";
        // Invalid JSON makes an accidental model call fail.
        var verifier = new LlmClaimEntailmentVerifier(new FakeChatClient("must not be called"));

        var outcome = await verifier.VerifyAsync(passage, passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_CompleteAssignedSegmentCopyWithCitationMarkers_IsEntailed()
    {
        const string passage = "They indicate that authorization is required [S1].";
        var verifier = new LlmClaimEntailmentVerifier(new FakeChatClient("must not be called"));

        var outcome = await verifier.VerifyAsync(
            "They indicate that authorization is required.", passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ChangedNegation_DoesNotUseCopyShortcut()
    {
        const string passage = "The sources do not confirm implementation.";
        var verifier = CreateVerifier(
            "The sources", "The sources", ClaimEntailmentOutcome.NotEstablished,
            supportingSegment: passage);

        var outcome = await verifier.VerifyAsync(
            "The sources confirm implementation.", passage, passage);

        Assert.AreEqual(ClaimEntailmentOutcome.NotEstablished, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_AttributionQuoteOmitsContextCitationMarker_ReturnsEntailed()
    {
        const string assigned = "but do not confirm their implementation.";
        const string context =
            "The sources recommend authorization controls [S1], but do not confirm their implementation.";
        var verifier = CreateVerifier(
            "The sources recommend authorization controls, but do not confirm",
            "The sources do not confirm", supportingSegment: assigned);

        var outcome = await verifier.VerifyAsync(
            "The sources do not confirm implementation of authorization controls.",
            assigned, context);

        Assert.AreEqual(ClaimEntailmentOutcome.Entailed, outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_InventedAttributionDespiteContextCitationMarker_Throws()
    {
        const string assigned = "but do not confirm their implementation.";
        const string context =
            "The sources recommend authorization controls [S1], but do not confirm their implementation.";
        var verifier = CreateVerifier(
            "The auditors do not confirm", "The auditors do not confirm",
            supportingSegment: assigned);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => verifier.VerifyAsync(
            "The auditors do not confirm implementation of authorization controls.",
            assigned, context));
    }

    private static LlmClaimEntailmentVerifier CreateVerifier(
        string? passageAttribution, string? claimAttribution,
        ClaimEntailmentOutcome outcome = ClaimEntailmentOutcome.Entailed,
        string? supportingSegment = Passage,
        bool? sourceAttributionRequired = null)
    {
        string response = JsonSerializer.Serialize(new
        {
            sourceAttributionRequired = sourceAttributionRequired ?? !string.IsNullOrWhiteSpace(passageAttribution),
            passageAttribution,
            claimAttribution,
            supportingSegment,
            outcome = outcome.ToString(),
            explanation = "Simulated model verdict."
        });
        return new LlmClaimEntailmentVerifier(new FakeChatClient(response));
    }
}
