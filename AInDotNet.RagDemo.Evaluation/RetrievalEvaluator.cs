using AInDotNet.RagDemo.Tes.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public static class RetrievalEvaluator
{
    public static RetrievalEvaluationResult Evaluate(RagEvaluationCase evaluationCase, IReadOnlyList<int> retrievedChunkIds, int topK)
    {
        ArgumentNullException.ThrowIfNull(evaluationCase);
        ArgumentNullException.ThrowIfNull(retrievedChunkIds);

        if (topK <= 0)
            throw new ArgumentOutOfRangeException(nameof(topK));

        var expected = evaluationCase.ExpectedChunkIds.ToHashSet();

        var retrieved = retrievedChunkIds.Take(topK).ToList();
        if (retrieved.Count != retrieved.Distinct().Count())
        {
            throw new ArgumentException("Retrieved chunk IDs must be unique.", nameof(retrievedChunkIds));
        }

        int relevantCount = retrieved.Count(expected.Contains);

        double precision = (double)relevantCount / topK;

        double recall = expected.Count == 0 ? 0 : (double)relevantCount / expected.Count;

        int firstRelevantRank = retrieved.FindIndex(expected.Contains);

        double reciprocalRank = firstRelevantRank < 0 ? 0 : 1.0 / (firstRelevantRank + 1);

        return new RetrievalEvaluationResult
        {
            EvaluationCaseId = evaluationCase.Id,
            TopK = topK,
            RelevantChunkCount = expected.Count,
            RetrievedRelevantCount = relevantCount,
            PrecisionAtK = precision,
            RecallAtK = recall,
            ReciprocalRank = reciprocalRank
        };
    }
}