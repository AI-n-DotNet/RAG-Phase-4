using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class CitedClaim
{
    public required string Claim { get; init; }

    public IReadOnlyList<string> CitationLabels { get; init; } = [];

    public string? OriginalText { get; init; }

    public string? ContextText { get; init; }
}