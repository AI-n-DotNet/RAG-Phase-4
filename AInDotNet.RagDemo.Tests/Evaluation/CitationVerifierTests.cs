using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using AInDotNet.RagDemo.Evaluation;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
public sealed class CitationVerifierTests
{
    [TestMethod]
    public async Task VerifyAsync_NoCitedClaims_ReturnsEmptyResult()
    {
        // Arrange
        var claimExtractor = new FakeCitedClaimExtractor([]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED")));

        var evidenceVerifier = new LlmClaimEvidenceVerifier(new FakeChatClient("SUPPORTED"));

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-001",
            Question = "How does enterprise RAG enforce authorization?",
            Answer = "Enterprise RAG requires authorization.",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual("TEST-001", result.EvaluationCaseId);
        Assert.AreEqual(0, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(0, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);
    }

    [TestMethod]
    public async Task VerifyAsync_SupportedCitation_ReturnsSupportedResult()
    {
        // Arrange
        var source = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S1"],
            OriginalText = "Enterprise RAG requires authorization [S1]."
        };

        var claimExtractor = new FakeCitedClaimExtractor([citedClaim]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED")));

        var evidenceVerifier = new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported);

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-002",
            Question = "How does enterprise RAG enforce authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual("TEST-002", result.EvaluationCaseId);
        CollectionAssert.AreEqual(
            new[] { EvidenceScope.IndividualSource }, evidenceVerifier.ReceivedScopes.ToArray());
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(1, result.SupportedCitations);
        Assert.AreEqual(0, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(1.0, result.CitationAccuracy);

        Assert.AreEqual("S1", result.Citations[0].SourceLabel);
        Assert.AreEqual(CitationVerificationOutcome.Supported, result.Citations[0].Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_InsufficientEvidence_ReturnsUnsupportedResult()
    {
        // Arrange
        var source = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems use vector search.",
            0.25);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S1"],
            OriginalText = "Enterprise RAG requires authorization [S1]."
        };

        var claimExtractor = new FakeCitedClaimExtractor([citedClaim]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED")));

        var evidenceVerifier = new FakeClaimEvidenceVerifier(ClaimSupportOutcome.InsufficientEvidence);

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-003",
            Question = "How does enterprise RAG enforce authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(1, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);

        Assert.AreEqual("S1", result.Citations[0].SourceLabel);
        Assert.AreEqual(CitationVerificationOutcome.Unsupported, result.Citations[0].Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_UnknownSource_ReturnsUnknownSourceResult()
    {
        // Arrange
        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S9"],
            OriginalText = "Enterprise RAG requires authorization [S9]."
        };

        var claimExtractor = new FakeCitedClaimExtractor([citedClaim]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED")));

        var evidenceVerifier = new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported);

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-004",
            Question = "How does enterprise RAG enforce authorization?",
            Answer = "Enterprise RAG requires authorization [S9].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(0, result.UnsupportedCitations);
        Assert.AreEqual(1, result.UnknownSourceCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);

        Assert.AreEqual("S9", result.Citations[0].SourceLabel);

        Assert.AreEqual(CitationVerificationOutcome.UnknownSource, result.Citations[0].Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_MultipleCitations_ReturnsMixedResults()
    {
        // Arrange
        var source1 = new RagSource(
            "S1", 101, 10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var source5 = new RagSource(
            "S5", 105, 20,
            "Vector Search Architecture",
            "Vector search retrieves semantically similar documents.",
            0.35);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S1", "S5"],
            OriginalText = "Enterprise RAG requires authorization [S1, S5]."
        };

        var claimExtractor = new FakeCitedClaimExtractor([citedClaim]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED")));

        var evidenceVerifier = new FakeClaimEvidenceVerifier(new Dictionary<string, ClaimSupportOutcome>
        {
            ["S1"] = ClaimSupportOutcome.Supported,
            ["S5"] = ClaimSupportOutcome.InsufficientEvidence
        });

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-005",
            Question = "How does enterprise RAG enforce authorization?",
            Answer = "Enterprise RAG requires authorization [S1, S5].",
            RetrievedSources = [source1, source5],
            CitedSources = [source1, source5],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(2, result.TotalCitations);
        Assert.AreEqual(1, result.SupportedCitations);
        Assert.AreEqual(1, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(0.5, result.CitationAccuracy);

        Assert.AreEqual("S1", result.Citations[0].SourceLabel);
        Assert.AreEqual(CitationVerificationOutcome.Supported, result.Citations[0].Outcome);

        Assert.AreEqual("S5", result.Citations[1].SourceLabel);
        Assert.AreEqual(CitationVerificationOutcome.Unsupported, result.Citations[1].Outcome);
    }

    [TestMethod]
    public async Task VerifyAsync_ContradictedEvidence_ReturnsUnsupportedResult()
    {
        // Arrange
        var source = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG does not require authorization.",
            0.25);

        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S1"],
            OriginalText = "Enterprise RAG requires authorization [S1]."
        };

        var claimExtractor = new FakeCitedClaimExtractor([citedClaim]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED")));

        var evidenceVerifier = new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Contradicted);

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-006",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(1, result.UnsupportedCitations);
        Assert.AreEqual(0, result.UnknownSourceCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);

        Assert.AreEqual(CitationVerificationOutcome.Unsupported, result.Citations[0].Outcome);

        Assert.AreEqual("Simulated evidence outcome: Contradicted", result.Citations[0].Explanation);
    }

    [TestMethod]
    public async Task VerifyAsync_InvalidClaimSpan_ThrowsException()
    {
        // Arrange
        var citedClaim = new CitedClaim
        {
            Claim = "Enterprise RAG requires authorization.",
            CitationLabels = ["S5"],
            OriginalText = "OAuth 2.0 provides delegated authorization [S5]."
        };

        var claimExtractor = new FakeCitedClaimExtractor([citedClaim]);

        var semanticValidator = new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("UNSUPPORTED")));

        var evidenceVerifier = new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported);

        var claimClassifier = new FakeClaimClassifier();

        var verifier = new CitationVerifier(claimExtractor, semanticValidator, evidenceVerifier, claimClassifier);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-007",
            Question = "Does enterprise RAG require authorization?",
            Answer = "OAuth 2.0 provides delegated authorization [S5].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act and Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => verifier.VerifyAsync(evaluationRun));

        Assert.AreEqual("Cited claim does not match its original answer passage: Enterprise RAG requires authorization.", exception.Message);
    }

    [TestMethod]
    public async Task VerifyAsync_UncitedClaim_DoesNotAffectCitationAccuracy()
    {
        // Arrange
        var source = new RagSource(
            "S1", 101, 10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var claims = new[]
        {
            new CitedClaim
            {
                Claim = "Enterprise RAG requires authorization.",
                CitationLabels = ["S1"],
                OriginalText = "Enterprise RAG requires authorization [S1]."
            },
            new CitedClaim
            {
                Claim = "OAuth 2.0 provides delegated authorization.",
                CitationLabels = [],
                OriginalText = "OAuth 2.0 provides delegated authorization."
            }
        };

        //var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported));
        var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported), new FakeClaimClassifier());

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-008",
            Question = "What authorization mechanisms are used?",
            Answer = """
            Enterprise RAG requires authorization [S1].
            OAuth 2.0 provides delegated authorization.
            """,
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(1, result.SupportedCitations);
        Assert.AreEqual(1.0, result.CitationAccuracy);

        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(1, result.CitedClaims);
        Assert.AreEqual(0.5, result.CitationCoverage);
    }

    [TestMethod]
    public async Task VerifyAsync_NoClaims_ReturnsZeroCoverage()
    {
        // Arrange
        //var verifier = new CitationVerifier(new FakeCitedClaimExtractor([]), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported));
        var verifier = new CitationVerifier(new FakeCitedClaimExtractor([]), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported), new FakeClaimClassifier());

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-NO-CLAIMS",
            Question = "Can you help me?",
            Answer = "Certainly! How can I help?",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(0, result.TotalClaims);
        Assert.AreEqual(0, result.CitedClaims);
        Assert.AreEqual(0.0, result.CitationCoverage);

        Assert.AreEqual(0, result.TotalCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);
    }

    [TestMethod]
    public async Task VerifyAsync_AllClaimsCited_ReturnsFullCoverage()
    {
        // Arrange
        var source1 = new RagSource(
            "S1", 101, 10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var source5 = new RagSource(
            "S5", 105, 20,
            "OAuth Architecture",
            "OAuth 2.0 provides delegated authorization.",
            0.35);

        var claims = new[]
        {
            new CitedClaim
            {
                Claim = "Enterprise RAG requires authorization.",
                CitationLabels = ["S1"],
                OriginalText = "Enterprise RAG requires authorization [S1]."
            },
            new CitedClaim
            {
                Claim = "OAuth 2.0 provides delegated authorization.",
                CitationLabels = ["S5"],
                OriginalText = "OAuth 2.0 provides delegated authorization [S5]."
            }
        };

        //var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported));
        var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported), new FakeClaimClassifier());

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-FULL-COVERAGE",
            Question = "What authorization mechanisms are relevant?",
            Answer = """
            Enterprise RAG requires authorization [S1].
            OAuth 2.0 provides delegated authorization [S5].
            """,
            RetrievedSources = [source1, source5],
            CitedSources = [source1, source5],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(2, result.CitedClaims);
        Assert.AreEqual(1.0, result.CitationCoverage);

        Assert.AreEqual(2, result.TotalCitations);
        Assert.AreEqual(2, result.SupportedCitations);
        Assert.AreEqual(1.0, result.CitationAccuracy);
    }

    [TestMethod]
    public async Task VerifyAsync_AllClaimsCitedButUnsupported_ReturnsZeroAccuracy()
    {
        // Arrange
        var source = new RagSource(
            "S1", 101, 10,
            "Vector Search Architecture",
            "Vector search retrieves semantically similar document chunks.",
            0.25);

        var claims = new[]
        {
            new CitedClaim
            {
                Claim = "Enterprise RAG requires authorization.",
                CitationLabels = ["S1"],
                OriginalText = "Enterprise RAG requires authorization [S1]."
            }
        };

        //var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.InsufficientEvidence));
        var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.InsufficientEvidence), new FakeClaimClassifier());

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-FULL-COVERAGE-ZERO-ACCURACY",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalClaims);
        Assert.AreEqual(1, result.CitedClaims);
        Assert.AreEqual(1.0, result.CitationCoverage);

        Assert.AreEqual(1, result.TotalCitations);
        Assert.AreEqual(0, result.SupportedCitations);
        Assert.AreEqual(1, result.UnsupportedCitations);
        Assert.AreEqual(0.0, result.CitationAccuracy);

        // Assert: Factual citation coverage
        Assert.AreEqual(1, result.TotalFactualClaims);
        Assert.AreEqual(1, result.CitedFactualClaims);
        Assert.AreEqual(1.0, result.FactualCitationCoverage);
    }

    [TestMethod]
    public void FactualCitationCoverage_ExcludesUncitedEvidenceScopeClaims()
    {
        // Arrange
        var result = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-FACTUAL-COVERAGE",
            Citations = [],
            TotalClaims = 3,
            CitedClaims = 1,
            TotalFactualClaims = 1,
            CitedFactualClaims = 1
        };

        // Act
        double overallCoverage = result.CitationCoverage;
        double factualCoverage = result.FactualCitationCoverage;

        // Assert
        Assert.AreEqual(1.0 / 3.0, overallCoverage);
        Assert.AreEqual(1.0, factualCoverage);
    }

    [TestMethod]
    public async Task VerifyAsync_UncitedCollectionEvidence_DoesNotReduceFactualCoverage()
    {
        // Arrange
        const string factualClaim = "Enterprise RAG requires authorization.";

        const string evidenceClaim = "The retrieved passages do not describe implementation.";

        var claims = new[]
        {
        new CitedClaim
        {
            Claim = factualClaim,
            CitationLabels = ["S1"],
            OriginalText = "Enterprise RAG requires authorization [S1]."
        },
        new CitedClaim
        {
            Claim = evidenceClaim,
            CitationLabels = [],
            OriginalText =                "The retrieved passages do not describe implementation."
        }
    };

        var classifier = new FakeClaimClassifier(
            new Dictionary<string, ClaimType>
            {
                [factualClaim] = ClaimType.Factual,
                [evidenceClaim] = ClaimType.CollectionEvidence
            });

        var verifier = new CitationVerifier(new FakeCitedClaimExtractor(claims), new CitedClaimSemanticValidator(new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))), new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported), classifier);

        var source = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG requires authorization.",
            0.25);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-MIXED-CITATION-COVERAGE",
            Question = "What does enterprise RAG require?",
            Answer = "Enterprise RAG requires authorization [S1]. " + "The retrieved passages do not describe implementation.",
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act
        CitationVerificationResult result = await verifier.VerifyAsync(evaluationRun);

        // Assert
        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(1, result.CitedClaims);
        Assert.AreEqual(0.5, result.CitationCoverage);

        Assert.AreEqual(1, result.TotalFactualClaims);
        Assert.AreEqual(1, result.CitedFactualClaims);
        Assert.AreEqual(1.0, result.FactualCitationCoverage);
    }

    [TestMethod]
    public async Task VerifyAsync_ClassifiesEachClaimExactlyOnce()
    {
        const string citedClaim = "Enterprise RAG requires authorization.";
        const string uncitedClaim = "Enterprise RAG requires auditing.";
        var claims = new[]
        {
            new CitedClaim
            {
                Claim = citedClaim,
                CitationLabels = ["S1"],
                OriginalText = "Enterprise RAG requires authorization [S1]."
            },
            new CitedClaim
            {
                Claim = uncitedClaim,
                CitationLabels = [],
                OriginalText = uncitedClaim
            }
        };

        var classifier = new CountingClaimClassifier();
        var verifier = new CitationVerifier(
            new FakeCitedClaimExtractor(claims),
            new CitedClaimSemanticValidator(
                new LlmClaimSpanVerifier(new FakeChatClient("SUPPORTED"))),
            new FakeClaimEvidenceVerifier(ClaimSupportOutcome.Supported),
            classifier);

        var source = new RagSource(
            "S1", 101, 10, "Enterprise RAG",
            "Enterprise RAG requires authorization.", 0.25);
        var run = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-CLASSIFICATION-COUNT",
            Question = "What does enterprise RAG require?",
            Answer = "Enterprise RAG requires authorization [S1]. " + uncitedClaim,
            RetrievedSources = [source],
            CitedSources = [source],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        CitationVerificationResult result = await verifier.VerifyAsync(run);

        Assert.AreEqual(2, classifier.ClassifiedClaims.Count);
        Assert.AreEqual(1, classifier.ClassifiedClaims.Count(claim => claim == citedClaim));
        Assert.AreEqual(1, classifier.ClassifiedClaims.Count(claim => claim == uncitedClaim));
        Assert.AreEqual(2, result.TotalFactualClaims);
        Assert.AreEqual(1, result.CitedFactualClaims);
        Assert.AreEqual(0.5, result.FactualCitationCoverage);
        Assert.AreEqual(1.0, result.CitationAccuracy);
    }

    private sealed class CountingClaimClassifier : IClaimClassifier
    {
        public List<string> ClassifiedClaims { get; } = [];

        public Task<ClaimType> ClassifyAsync(
            string claim, CancellationToken cancellationToken = default)
        {
            ClassifiedClaims.Add(claim);
            return Task.FromResult(ClaimType.Factual);
        }
    }
}