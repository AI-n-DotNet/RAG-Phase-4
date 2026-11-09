using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class CitedClaimSemanticValidatorTests
{
    [TestMethod]
    public async Task ValidateAsync_ValidAssociation_ReturnsTrue()
    {
        // Arrange
        var spanVerifier = new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"));

        var validator = new CitedClaimSemanticValidator(spanVerifier);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S1"],
            OriginalText = "Enterprise RAG requires authorization [S1]."
        };

        // Act
        bool result = await validator.ValidateAsync(citedClaim);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ValidateAsync_InvalidAssociation_ReturnsFalse()
    {
        // Arrange
        var spanVerifier = new LlmClaimSpanVerifier(new FakeChatClient("UNSUPPORTED"));

        var validator = new CitedClaimSemanticValidator(spanVerifier);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S5"],
            OriginalText = "OAuth 2.0 provides delegated authorization [S5]."
        };

        // Act
        bool result = await validator.ValidateAsync(citedClaim);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ValidateAsync_MissingOriginalText_ReturnsFalse()
    {
        // Arrange
        var spanVerifier = new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"));

        var validator = new CitedClaimSemanticValidator(spanVerifier);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S1"],
            OriginalText = null
        };

        // Act
        bool result = await validator.ValidateAsync(citedClaim);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ValidateAsync_UsesContextTextWhenAvailable()
    {
        // Arrange
        var verifier = new CapturingClaimSpanVerifier();
        var validator = new CitedClaimSemanticValidator(verifier);

        string sentence = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        var citedClaim = new CitedClaim
        {
            Claim = "The sources do not confirm implementation.",
            OriginalText = "but do not confirm their implementation.",
            ContextText = sentence,
            CitationLabels = []
        };

        // Act
        bool result = await validator.ValidateAsync(citedClaim);

        // Assert
        Assert.IsTrue(result);

        Assert.AreEqual("but do not confirm their implementation.", verifier.ReceivedOriginalText);

        Assert.AreEqual(sentence, verifier.ReceivedContextText);

        Assert.AreEqual(0, citedClaim.CitationLabels.Count);
    }

    private sealed class CapturingClaimSpanVerifier : IClaimSpanVerifier
    {
        public string? ReceivedOriginalText { get; private set; }

        public string? ReceivedContextText { get; private set; }

        public Task<bool> VerifyAsync(string claim, string originalText, CancellationToken cancellationToken = default)
        {
            ReceivedOriginalText = originalText;
            return Task.FromResult(true);
        }

        public Task<bool> VerifyAsync(string claim, string originalText, string contextText, CancellationToken cancellationToken = default)
        {
            ReceivedOriginalText = originalText;
            ReceivedContextText = contextText;

            return Task.FromResult(true);
        }
    }

    private sealed class SegmentAwareFakeVerifier : IClaimSpanVerifier
    {
        public Task<bool> VerifyAsync(string claim, string originalText, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> VerifyAsync(string claim, string originalText, string contextText, CancellationToken cancellationToken = default)
        {
            bool supported = originalText.Contains("recommend authorization controls", StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(supported);
        }
    }

    [TestMethod]
    public async Task ValidateAsync_UsesOriginalTextWhenContextIsMissing()
    {
        // Arrange
        var verifier = new CapturingClaimSpanVerifier();
        var validator = new CitedClaimSemanticValidator(verifier);

        string originalText = "Enterprise RAG systems must enforce authorization [S1].";

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG systems must enforce authorization.",
            OriginalText = originalText,
            CitationLabels = ["S1"]
        };

        // Act
        bool result = await validator.ValidateAsync(citedClaim);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(originalText, verifier.ReceivedOriginalText);
        Assert.AreEqual(1, citedClaim.CitationLabels.Count);
        Assert.AreEqual("S1", citedClaim.CitationLabels[0]);
    }

    [TestMethod]
    public async Task ValidateAsync_ClaimFromDifferentClause_ReturnsFalse()
    {
        // Arrange
        //var verifier = new CapturingClaimSpanVerifier();
        var verifier = new SegmentAwareFakeVerifier();
        var validator = new CitedClaimSemanticValidator(verifier);

        string sentence = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        var citedClaim = new CitedClaim
        {
            Claim = "The sources recommend authorization controls.",
            OriginalText = "but do not confirm their implementation.",
            ContextText = sentence,
            CitationLabels = []
        };

        // Act
        bool result = await validator.ValidateAsync(citedClaim);

        // Assert
        Assert.IsFalse(result);
    }
}