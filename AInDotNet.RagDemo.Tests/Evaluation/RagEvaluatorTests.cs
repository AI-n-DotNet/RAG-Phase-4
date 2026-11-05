using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public sealed class RagEvaluatorTests
{
    [TestMethod]
    public async Task EvaluateAsync_CombinesFaithfulnessAndCitationResults()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-001",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-001",
            Citations = [],
            TotalClaims = 2,
            CitedClaims = 1
        };

        var fakeFaithfulness = new FakeFaithfulnessEvaluator(faithfulnessResult);

        var fakeCitations = new FakeCitationVerifier(citationResult);

        var evaluator = new RagEvaluator(fakeFaithfulness, fakeCitations);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-001",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 5
        };

        // Act
        RagEvaluationResult result = await evaluator.EvaluateAsync(evaluationRun);

        // Assert
        Assert.AreEqual("TEST-001", result.EvaluationCaseId);

        Assert.AreSame(faithfulnessResult, result.Faithfulness);

        Assert.AreSame(citationResult, result.Citations);

        Assert.AreEqual(1, fakeFaithfulness.CallCount);
        Assert.AreEqual(1, fakeCitations.CallCount);

        Assert.AreEqual(0.5, result.Citations.CitationCoverage);

        Assert.AreEqual(0.5, result.Citations.CitationCoverage);
        Assert.AreEqual(0.0, result.Citations.CitationAccuracy);
        Assert.AreEqual(0.0, result.Faithfulness.FaithfulnessScore);
    }

    [TestMethod]
    public async Task EvaluateAsync_NullEvaluationRun_ThrowsArgumentNullException()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-NULL",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-NULL",
            Citations = []
        };

        var fakeFaithfulness =
            new FakeFaithfulnessEvaluator(faithfulnessResult);

        var fakeCitations = new FakeCitationVerifier(citationResult);

        var evaluator = new RagEvaluator(fakeFaithfulness, fakeCitations);

        // Act
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => evaluator.EvaluateAsync(null!));

        // Assert
        Assert.AreEqual(0, fakeFaithfulness.CallCount);
        Assert.AreEqual(0, fakeCitations.CallCount);
    }

    [TestMethod]
    public async Task EvaluateAsync_MismatchedEvaluationCaseId_ThrowsException()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "WRONG-CASE",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-001",
            Citations = []
        };

        var evaluator = new RagEvaluator(new FakeFaithfulnessEvaluator(faithfulnessResult), new FakeCitationVerifier(citationResult));

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-001",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act and Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => evaluator.EvaluateAsync(evaluationRun));
    }

    [TestMethod]
    public async Task EvaluateAsync_MismatchedCitationCaseId_ThrowsException()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-001",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "WRONG-CASE",
            Citations = []
        };

        var fakeFaithfulness = new FakeFaithfulnessEvaluator(faithfulnessResult);

        var fakeCitations = new FakeCitationVerifier(citationResult);

        var evaluator = new RagEvaluator(fakeFaithfulness, fakeCitations);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-001",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act and Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => evaluator.EvaluateAsync(evaluationRun));

        Assert.AreEqual(1, fakeFaithfulness.CallCount);
        Assert.AreEqual(1, fakeCitations.CallCount);
    }

    [TestMethod]
    public async Task EvaluateAsync_ForwardsCancellationTokenToBothEvaluators()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-CANCELLATION",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-CANCELLATION",
            Citations = []
        };

        var fakeFaithfulness = new FakeFaithfulnessEvaluator(faithfulnessResult);

        var fakeCitations = new FakeCitationVerifier(citationResult);

        var evaluator = new RagEvaluator(fakeFaithfulness, fakeCitations);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-CANCELLATION",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        using var cancellationTokenSource = new CancellationTokenSource();

        CancellationToken token = cancellationTokenSource.Token;

        // Act
        await evaluator.EvaluateAsync(evaluationRun, token);

        // Assert
        Assert.AreEqual(token, fakeFaithfulness.LastCancellationToken);

        Assert.AreEqual(token, fakeCitations.LastCancellationToken);

        Assert.AreEqual(1, fakeFaithfulness.CallCount);
        Assert.AreEqual(1, fakeCitations.CallCount);
    }

    [TestMethod]
    public async Task EvaluateAsync_CanceledAfterFaithfulness_SkipsCitationVerification()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-CANCEL-BETWEEN",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-CANCEL-BETWEEN",
            Citations = []
        };

        using var cancellationTokenSource = new CancellationTokenSource();

        var fakeFaithfulness = new FakeFaithfulnessEvaluator(faithfulnessResult)
        {
            OnEvaluate = () => cancellationTokenSource.Cancel()
        };

        var fakeCitations = new FakeCitationVerifier(citationResult);

        var evaluator = new RagEvaluator(fakeFaithfulness, fakeCitations);
        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-CANCEL-BETWEEN",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        // Act and Assert
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => evaluator.EvaluateAsync(evaluationRun, cancellationTokenSource.Token));

        Assert.AreEqual(1, fakeFaithfulness.CallCount);
        Assert.AreEqual(0, fakeCitations.CallCount);
    }

    [TestMethod]
    public async Task EvaluateAsync_PreCanceledToken_SkipsBothEvaluators()
    {
        // Arrange
        var faithfulnessResult = new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = "TEST-PRE-CANCEL",
            Claims = []
        };

        var citationResult = new CitationVerificationResult
        {
            EvaluationCaseId = "TEST-PRE-CANCEL",
            Citations = []
        };

        var fakeFaithfulness = new FakeFaithfulnessEvaluator(faithfulnessResult);

        var fakeCitations = new FakeCitationVerifier(citationResult);

        var evaluator = new RagEvaluator(fakeFaithfulness, fakeCitations);

        var evaluationRun = new RagEvaluationRun
        {
            EvaluationCaseId = "TEST-PRE-CANCEL",
            Question = "Does enterprise RAG require authorization?",
            Answer = "Enterprise RAG requires authorization [S1].",
            RetrievedSources = [],
            CitedSources = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = 1
        };

        using var cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act and Assert
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => evaluator.EvaluateAsync(evaluationRun, cancellationTokenSource.Token));

        Assert.AreEqual(0, fakeFaithfulness.CallCount);
        Assert.AreEqual(0, fakeCitations.CallCount);
    }
}