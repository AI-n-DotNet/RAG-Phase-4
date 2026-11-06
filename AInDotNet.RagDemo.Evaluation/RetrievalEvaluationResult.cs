using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class RetrievalEvaluationResult
{
    public required string EvaluationCaseId { get; init; }

    public required int TopK { get; init; }

    public required int RelevantChunkCount { get; init; }

    public required int RetrievedRelevantCount { get; init; }

    public required double PrecisionAtK { get; init; }

    public required double RecallAtK { get; init; }

    public required double ReciprocalRank { get; init; }
}