using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public enum AbstentionOutcome
{
    CorrectAbstention,
    IncorrectAssertion,
    AmbiguousResponse,
    IncorrectAbstention
}

public sealed class AbstentionEvaluationResult
{
    public required string EvaluationCaseId { get; init; }

    public required bool ExpectedAbstention { get; init; }

    public required AbstentionOutcome Outcome { get; init; }

    public required string Answer { get; init; }

    public string? Explanation { get; init; }
}