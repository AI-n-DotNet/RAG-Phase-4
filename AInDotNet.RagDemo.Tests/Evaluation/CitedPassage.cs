using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed record CitedPassage(int PassageId, string OriginalText, IReadOnlyList<string> CitationLabels)
{
    public string ContextText { get; init; } = OriginalText;
}