using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public sealed class FaithfulnessEvaluatorTests
{

    [TestMethod]
    public async Task EvaluateAsync_UsesRetrievedSourcesAndCalculatesScore()
    {
        // Arrange
        var retrievedSources = new List<RagSource>
        {
            new(
                "S1", 69, 1,
                "Enterprise RAG Architecture",
                "Enterprise RAG systems must enforce authorization.",
                0.37),

            new(
                "S2", 100, 1,
                "Security Implementation",
                "The demonstration does not implement document-level authorization.",
                0.45)
        };

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-001",
            Question = "What authorization controls are implemented?",
            Answer = "Enterprise RAG must enforce authorization, " +
                     "and this demonstration implements it.",
            RetrievedSources = retrievedSources,
            CitedSources = [retrievedSources[0]],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 2
        };

        var extractor = new FakeClaimExtractor(
        [
            "Enterprise RAG must enforce authorization.",
            "This demonstration implements document-level authorization."
        ]);

        var verifier = new FakeClaimEvidenceVerifier();

        var normalizer = new FakeClaimNormalizer();

        var evaluator = new FaithfulnessEvaluator(extractor, normalizer, verifier);

        // Act
        var result = await evaluator.EvaluateAsync(evaluationRun);

        // Assert
        Assert.AreEqual("TEST-001", result.EvaluationCaseId);
        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(1, result.SupportedClaims);
        Assert.AreEqual(0.5, result.FaithfulnessScore, 0.000001);

        Assert.AreEqual(ClaimSupportOutcome.Supported, result.Claims[0].Outcome);

        Assert.AreEqual(ClaimSupportOutcome.Contradicted, result.Claims[1].Outcome);

        // Both retrieved passages must reach the verifier,
        // even though only S1 was cited.
        Assert.AreEqual(2, verifier.ReceivedSources.Count);
        CollectionAssert.AreEqual(
            new[] { EvidenceScope.CompleteRetrievedSet, EvidenceScope.CompleteRetrievedSet },
            verifier.ReceivedScopes.ToArray());

        CollectionAssert.AreEquivalent(new[] { "S1", "S2" }, verifier.ReceivedSources.Select(source => source.Label).ToArray());
    }

    private sealed class FakeClaimExtractor : IClaimExtractor
    {
        private readonly IReadOnlyList<string> _claims;

        public FakeClaimExtractor(IReadOnlyList<string> claims)
        {
            _claims = claims;
        }

        public Task<IReadOnlyList<string>> ExtractClaimsAsync(string answer, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_claims);
        }
    }

    private sealed class FakeClaimEvidenceVerifier : IClaimEvidenceVerifier
    {
        public IReadOnlyList<RagSource> ReceivedSources { get; private set; } = [];

        public List<EvidenceScope> ReceivedScopes { get; } = [];

        public Task<ClaimEvaluationResult> VerifyAsync(string claim, IReadOnlyList<RagSource> sources, EvidenceScope evidenceScope, CancellationToken cancellationToken = default)
        {
            ReceivedSources = sources;
            ReceivedScopes.Add(evidenceScope);

            bool isRequirement = claim.Contains("must enforce", StringComparison.OrdinalIgnoreCase);

            var result = new ClaimEvaluationResult
            {
                Claim = claim,
                Outcome = isRequirement ? ClaimSupportOutcome.Supported : ClaimSupportOutcome.Contradicted,
                SupportingSourceLabels = isRequirement ? ["S1"] : ["S2"],
                Explanation = "Deterministic test classification."
            };

            return Task.FromResult(result);
        }
    }

    public sealed class FakeClaimNormalizer : IClaimNormalizer
    {
        public Task<IReadOnlyList<string>> NormalizeAsync(IReadOnlyList<string> claims, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(claims);
        }
    }

    public sealed class FakeDeduplicatingClaimNormalizer : IClaimNormalizer
    {
        public Task<IReadOnlyList<string>> NormalizeAsync(IReadOnlyList<string> claims, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>(claims.Take(1).ToList());
        }
    }

    [TestMethod]
    public async Task EvaluateAsync_DuplicateClaims_CountsEachOnce()
    {
        // Arrange
        //var evaluationRun = new RagEvaluationRun
        //{
        //    EvaluationCaseId = "TEST-DUPLICATES",
        //    Question = "What controls are implemented?",
        //    Answer = "Enterprise RAG must enforce authorization.",
        //    RetrievedSources = [],
        //    CitedSources = [],
        //    ExecutedAtUtc = DateTimeOffset.UtcNow,
        //    TopK = 5
        //};
        var retrievedSources = new List<RagSource>
        {
            new(
                "S1", 69, 1,
                "Enterprise RAG Architecture",
                "Enterprise RAG systems must enforce authorization.",
                0.37),

            new(
                "S2", 100, 1,
                "Security Implementation",
                "The demonstration does not implement document-level authorization.",
                0.45)
        };

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-001",
            Question = "What authorization controls are implemented?",
            Answer = "Enterprise RAG must enforce authorization, " +
                     "and this demonstration implements it.",
            RetrievedSources = retrievedSources,
            CitedSources = [retrievedSources[0]],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 2
        };


        var extractor = new FakeClaimExtractor(
        [
            "Enterprise RAG must enforce authorization.",
            "Enterprise RAG must enforce authorization.",
            "ENTERPRISE RAG MUST ENFORCE AUTHORIZATION."
        ]);

        var verifier = new FakeClaimEvidenceVerifier();

        var normalizer = new FakeClaimNormalizer();

        var evaluator = new FaithfulnessEvaluator(extractor, normalizer, verifier);

        // Act
        var result = await evaluator.EvaluateAsync(evaluationRun);

        // Assert
        Assert.AreEqual(1, result.TotalClaims);
        Assert.AreEqual(1, result.SupportedClaims);
        Assert.AreEqual(1.0, result.FaithfulnessScore);
    }

    [TestMethod]
    public async Task EvaluateAsync_NormalizedClaims_VerifiesOnlyRetainedClaims()
    {
        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-DUPLICATES",
            Question = "What controls are implemented?",
            Answer = "Enterprise RAG must enforce authorization.",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };
        var extractor = new FakeClaimExtractor(
        [
            "The retrieved sources do not identify implemented authorization controls.",
        "No passage confirms which authorization controls are implemented."
        ]);

        var normalizer = new FakeDeduplicatingClaimNormalizer();
        var verifier = new FakeClaimEvidenceVerifier();

        var evaluator = new FaithfulnessEvaluator(extractor, normalizer, verifier);

        // Reuse an existing RagEvaluationRun fixture or constructor from your other tests.
        var result = await evaluator.EvaluateAsync(evaluationRun);

        Assert.AreEqual(1, result.TotalClaims);
    }

    [TestMethod]
    public async Task VerifyAsync_DefaultScope_UsesCompleteRetrievedSet()
    {
        var capturingVerifier = new FakeClaimEvidenceVerifier();
        IClaimEvidenceVerifier verifier = capturingVerifier;
        IReadOnlyList<RagSource> sources =
        [
            new("S1", 1, 1, "Requirements",
                "Enterprise RAG must enforce authorization.", 0.1)
        ];

        ClaimEvaluationResult result = await verifier.VerifyAsync(
            "Enterprise RAG must enforce authorization.", sources);

        CollectionAssert.AreEqual(
            new[] { EvidenceScope.CompleteRetrievedSet },
            capturingVerifier.ReceivedScopes.ToArray());
        Assert.AreSame(sources, capturingVerifier.ReceivedSources);
        Assert.AreEqual(ClaimSupportOutcome.Supported, result.Outcome);
    }
}
