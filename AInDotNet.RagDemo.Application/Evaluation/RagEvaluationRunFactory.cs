using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Application.Evaluation;

public static class RagEvaluationRunFactory
{
    public static RagEvaluationRun Create(string evaluationCaseId, string question, RagAnswerResult answerResult, int topK)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationCaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(answerResult);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        return new RagEvaluationRun
        {
            EvaluationCaseId = evaluationCaseId,
            Question = question,
            Answer = answerResult.Answer,
            RetrievedSources = answerResult.RetrievedSources,
            CitedSources = answerResult.Sources,
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            TopK = topK
        };
    }
}