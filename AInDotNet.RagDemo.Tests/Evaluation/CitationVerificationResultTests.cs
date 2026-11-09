using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public class CitationVerificationResultTests
{

    [TestMethod]
    public void CitedClaimSupportRate_MixedCitationSupport_ReturnsOne()
    {
        // Arrange
        var result = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-001",
            TotalClaims = 2,
            CitedClaims = 2,
            Citations =
            [
                new CitationEvaluationResult
            {
                Claim = "Enterprise RAG requires authorization.",
                SourceLabel = "S1",
                Outcome = CitationVerificationOutcome.Supported
            },
            new CitationEvaluationResult
            {
                Claim = "Enterprise RAG requires authorization.",
                SourceLabel = "S5",
                Outcome = CitationVerificationOutcome.Unsupported
            },
            new CitationEvaluationResult
            {
                Claim = "OAuth 2.0 supports delegated authorization.",
                SourceLabel = "S1",
                Outcome = CitationVerificationOutcome.Unsupported
            },
            new CitationEvaluationResult
            {
                Claim = "OAuth 2.0 supports delegated authorization.",
                SourceLabel = "S5",
                Outcome = CitationVerificationOutcome.Supported
            }
            ]
        };

        // Act
        double supportRate = result.CitedClaimSupportRate;

        // Assert
        Assert.AreEqual(1.0, supportRate, 0.0001);
    }

    [TestMethod]
    public void CitedClaimSupportRate_NoCitations_ReturnsZero()
    {
        var result = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-002",
            TotalClaims = 1,
            CitedClaims = 0,
            Citations = []
        };

        Assert.AreEqual(0.0, result.CitedClaimSupportRate, 0.0001);
    }
}