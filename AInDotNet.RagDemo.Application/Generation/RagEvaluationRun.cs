using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Application.Generation;

public sealed class RagEvaluationRun
{
    public required string EvaluationCaseId { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    public required IReadOnlyList<RagSource> RetrievedSources { get; init; }
    
    public required IReadOnlyList<RagSource> CitedSources { get; init; }

    public required DateTimeOffset ExecutedAtUtc { get; init; }

    public required int TopK { get; init; }
}