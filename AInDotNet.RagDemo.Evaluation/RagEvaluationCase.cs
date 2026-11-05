using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class RagEvaluationCase
{
    public required string Id { get; init; }

    public required string Question { get; init; }

    public string? ExpectedAnswer { get; init; }

    public List<int> ExpectedChunkIds { get; init; } = [];

    public bool ShouldAbstain { get; init; }
}