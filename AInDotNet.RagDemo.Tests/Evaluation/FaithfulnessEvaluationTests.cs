using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public sealed class FaithfulnessEvaluationTests
{
    [TestMethod]
    public void FaithfulnessScore_AllClaimsSupported_ReturnsOne()
    {
        var result = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-001",
            Claims =
            [
                new()
                {
                    Claim = "Enterprise RAG requires authorization.",
                    Outcome = ClaimSupportOutcome.Supported
                },
                new()
                {
                    Claim = "OAuth supports delegated authorization.",
                    Outcome = ClaimSupportOutcome.Supported
                }
            ]
        };

        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(2, result.SupportedClaims);
        Assert.AreEqual(1.0, result.FaithfulnessScore, 0.000001);
    }

    [TestMethod]
    public void FaithfulnessScore_MixedClaims_CalculatesRatio()
    {
        var result = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-002",
            Claims =
            [
                new()
                {
                    Claim = "Enterprise RAG requires authorization.",
                    Outcome = ClaimSupportOutcome.Supported
                },
                new()
                {
                    Claim = "OAuth supports delegated authorization.",
                    Outcome = ClaimSupportOutcome.Supported
                },
                new()
                {
                    Claim = "Our demonstration implements OAuth.",
                    Outcome = ClaimSupportOutcome.InsufficientEvidence
                },
                new()
                {
                    Claim = "Our demonstration enforces RBAC.",
                    Outcome = ClaimSupportOutcome.InsufficientEvidence
                },
                new()
                {
                    Claim = "The documentation describes authorization requirements.",
                    Outcome = ClaimSupportOutcome.Supported
                }
            ]
        };

        Assert.AreEqual(5, result.TotalClaims);
        Assert.AreEqual(3, result.SupportedClaims);
        Assert.AreEqual(0.6, result.FaithfulnessScore, 0.000001);
    }

    [TestMethod]
    public void FaithfulnessScore_NoSupportedClaims_ReturnsZero()
    {
        var result = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-003",
            Claims =
            [
                new()
                {
                    Claim = "Our demonstration implements OAuth.",
                    Outcome = ClaimSupportOutcome.InsufficientEvidence
                },
                new()
                {
                    Claim = "Our demonstration enforces RBAC.",
                    Outcome = ClaimSupportOutcome.Contradicted
                }
            ]
        };

        Assert.AreEqual(2, result.TotalClaims);
        Assert.AreEqual(0, result.SupportedClaims);
        Assert.AreEqual(0.0, result.FaithfulnessScore, 0.000001);
    }

    [TestMethod]
    public void FaithfulnessScore_NoClaims_ReturnsZero()
    {
        var result = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-004",
            Claims = []
        };

        Assert.AreEqual(0, result.TotalClaims);
        Assert.AreEqual(0, result.SupportedClaims);
        Assert.AreEqual(0.0, result.FaithfulnessScore, 0.000001);
    }

    [TestMethod]
    public void AuthorizationReference_HasExpectedJudgments()
    {
        // Arrange
        var reference = FaithfulnessReferenceCases.Authorization;

        // Assert
        Assert.AreEqual("RAG-SECURITY-002", reference.EvaluationCaseId);

        Assert.AreEqual(3, reference.TotalClaims);
        Assert.AreEqual(2, reference.SupportedClaims);

        Assert.AreEqual(2.0 / 3.0, reference.FaithfulnessScore, 0.000001);

        Assert.AreEqual(ClaimSupportOutcome.Supported, reference.Claims[0].Outcome);

        Assert.AreEqual(ClaimSupportOutcome.Supported, reference.Claims[1].Outcome);

        Assert.AreEqual(ClaimSupportOutcome.InsufficientEvidence, reference.Claims[2].Outcome);

        Assert.IsTrue(reference.Claims[0].SupportingSourceLabels.Contains("S1"));

        Assert.IsTrue(reference.Claims[1].SupportingSourceLabels.Contains("S5"));
    }
}