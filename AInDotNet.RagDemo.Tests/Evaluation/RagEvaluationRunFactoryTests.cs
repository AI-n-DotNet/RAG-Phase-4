using AInDotNet.RagDemo.Application.Evaluation;
using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class RagEvaluationRunFactoryTests
{
    [TestMethod]
    public void Create_ValidAnswerResult_PreservesAllData()
    {
        // Arrange
        var retrievedSource = new RagSource(
            "S1",
            101,
            10,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization.",
            0.25);

        var citedSource = new RagSource(
            "S2",
            102,
            20,
            "OAuth Architecture",
            "OAuth 2.0 provides delegated authorization.",
            0.35);

        var answerResult = new RagAnswerResult("Enterprise RAG requires authorization [S2].", [citedSource], [retrievedSource, citedSource]);

        // Act
        RagEvaluationRun result = RagEvaluationRunFactory.Create("EVAL-001", "Does enterprise RAG require authorization?", answerResult, topK: 5);

        // Assert
        Assert.AreEqual("EVAL-001", result.EvaluationCaseId);

        Assert.AreEqual("Does enterprise RAG require authorization?", result.Question);

        Assert.AreEqual(answerResult.Answer, result.Answer);

        Assert.AreSame(answerResult.Sources, result.CitedSources);

        Assert.AreSame(answerResult.RetrievedSources, result.RetrievedSources);

        Assert.AreEqual(5, result.TopK);

        Assert.IsTrue(result.ExecutedAtUtc <= DateTimeOffset.UtcNow);
    }

    [TestMethod]
    public void Create_InvalidTopK_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var answerResult = new RagAnswerResult("Enterprise RAG requires authorization [S1].", [], []);

        // Act and Assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RagEvaluationRunFactory.Create("EVAL-INVALID-TOPK", "Does enterprise RAG require authorization?", answerResult, topK: 0));
    }
}