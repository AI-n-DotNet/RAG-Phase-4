using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public sealed class AbstentionEvaluationTests
{
    [TestMethod]
    public void AuthorizationImplementation_RecordsCorrectAbstention()
    {
        // Arrange
        var evaluationCase = RagEvaluationCases.AuthorizationImplementation;

        // Previously reviewed answer from our integration test.
        const string answer =
            "The retrieved sources do not provide explicit " +
            "information on the authorization controls currently " +
            "implemented in your RAG demonstration application. " +
            "They mention that enterprise RAG systems must enforce " +
            "authorization as a recommended security control, " +
            "but do not confirm its implementation in the " +
            "demonstration application specifically [S1].";

        // Act
        var result = new AbstentionEvaluationResult
        {
            EvaluationCaseId = evaluationCase.Id,
            ExpectedAbstention = evaluationCase.ShouldAbstain,
            Outcome = AbstentionOutcome.CorrectAbstention,
            Answer = answer,
            Explanation = "The answer correctly distinguishes documented " +
                "security requirements from verified implementations."
        };

        // Assert
        Assert.AreEqual("RAG-SECURITY-002", result.EvaluationCaseId);

        Assert.IsTrue(result.ExpectedAbstention);

        Assert.AreEqual(AbstentionOutcome.CorrectAbstention, result.Outcome);

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Answer));

        Console.WriteLine($"Evaluation: {result.EvaluationCaseId}");
        Console.WriteLine($"Outcome: {result.Outcome}");
        Console.WriteLine($"Explanation: {result.Explanation}");
    }
}