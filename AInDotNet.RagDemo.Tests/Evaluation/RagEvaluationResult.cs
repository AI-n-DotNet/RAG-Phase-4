using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class RagEvaluationResult
{
    public required string EvaluationCaseId { get; init; }

    public required FaithfulnessEvaluationResult Faithfulness { get; init; }

    public required CitationVerificationResult Citations { get; init; }
}