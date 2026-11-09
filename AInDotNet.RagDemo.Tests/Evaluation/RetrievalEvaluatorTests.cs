using AInDotNet.RagDemo.Evaluation;
using AInDotNet.RagDemo.Tests.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
[TestCategory("Deterministic")]
public sealed class RetrievalEvaluatorTests
{
    [TestMethod]
    public void Evaluate_WithTwoRelevantChunks_CalculatesCorrectMetrics()
    {
        // Arrange
        var evaluationCase = new RagEvaluationCase
        {
            Id = "RETRIEVAL-001",
            Question = "How does enterprise RAG enforce authorization?",
            ExpectedChunkIds = [25, 41, 63]
        };

        IReadOnlyList<int> retrievedChunkIds = [12, 25, 38, 41, 57];

        // Act
        var result = RetrievalEvaluator.Evaluate(evaluationCase, retrievedChunkIds, topK: 5);

        // Assert
        Assert.AreEqual("RETRIEVAL-001", result.EvaluationCaseId);
        Assert.AreEqual(5, result.TopK);
        Assert.AreEqual(3, result.RelevantChunkCount);
        Assert.AreEqual(2, result.RetrievedRelevantCount);

        Assert.AreEqual(0.40, result.PrecisionAtK, 0.0001);
        Assert.AreEqual(2.0 / 3.0, result.RecallAtK, 0.0001);
        Assert.AreEqual(0.50, result.ReciprocalRank, 0.0001);
    }

    [TestMethod]
    public void Evaluate_WithNoRelevantChunks_ReturnsZeroMetrics()
    {
        // Arrange
        var evaluationCase = new RagEvaluationCase
        {
            Id = "RETRIEVAL-002",
            Question = "How does enterprise RAG enforce authorization?",
            ExpectedChunkIds = [25, 41, 63]
        };

        IReadOnlyList<int> retrievedChunkIds = [10, 20, 30, 40, 50];

        // Act
        var result = RetrievalEvaluator.Evaluate(evaluationCase, retrievedChunkIds, topK: 5);

        // Assert
        Assert.AreEqual(0, result.RetrievedRelevantCount);
        Assert.AreEqual(0.0, result.PrecisionAtK);
        Assert.AreEqual(0.0, result.RecallAtK);
        Assert.AreEqual(0.0, result.ReciprocalRank);
    }


    [TestMethod]
    public void Evaluate_WithTopKSmallerThanResults_EvaluatesOnlyTopK()
    {
        // Arrange
        var evaluationCase = new RagEvaluationCase
        {
            Id = "RETRIEVAL-003",
            Question = "How does enterprise RAG enforce authorization?",
            ExpectedChunkIds = [25, 41, 63]
        };

        IReadOnlyList<int> retrievedChunkIds = [12, 25, 38, 41, 63];

        // Act
        var result = RetrievalEvaluator.Evaluate(evaluationCase, retrievedChunkIds, topK: 3);

        // Assert
        Assert.AreEqual(3, result.TopK);
        Assert.AreEqual(3, result.RelevantChunkCount);
        Assert.AreEqual(1, result.RetrievedRelevantCount);

        Assert.AreEqual(1.0 / 3.0, result.PrecisionAtK, 0.0001);
        Assert.AreEqual(1.0 / 3.0, result.RecallAtK, 0.0001);
        Assert.AreEqual(0.5, result.ReciprocalRank, 0.0001);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-10)]
    public void Evaluate_WithInvalidTopK_ThrowsException(int topK)
    {
        // Arrange
        var evaluationCase = new RagEvaluationCase
        {
            Id = "RETRIEVAL-004",
            Question = "How does enterprise RAG enforce authorization?",
            ExpectedChunkIds = [25, 41, 63]
        };

        IReadOnlyList<int> retrievedChunkIds = [12, 25, 38];

        // Act & Assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RetrievalEvaluator.Evaluate(evaluationCase, retrievedChunkIds, topK));
    }

    [TestMethod]
    public void Evaluate_WithDuplicateRetrievedChunks_ThrowsException()
    {
        // Arrange
        var evaluationCase = new RagEvaluationCase
        {
            Id = "RETRIEVAL-005",
            Question = "How does enterprise RAG enforce authorization?",
            ExpectedChunkIds = [25, 41]
        };

        IReadOnlyList<int> retrievedChunkIds = [25, 25, 25];

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() => RetrievalEvaluator.Evaluate(evaluationCase, retrievedChunkIds, topK: 3));
    }

}
