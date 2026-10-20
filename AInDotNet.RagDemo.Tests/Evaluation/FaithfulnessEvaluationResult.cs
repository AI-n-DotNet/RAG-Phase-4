using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public enum ClaimSupportOutcome
{
    Supported,
    Contradicted,
    InsufficientEvidence
}

public sealed class ClaimEvaluationResult
{
    public required string Claim { get; init; }

    public required ClaimSupportOutcome Outcome { get; init; }

    public List<string> SupportingSourceLabels { get; init; } = [];

    public string? Explanation { get; init; }
}

public sealed class FaithfulnessEvaluationResult
{
    public required string EvaluationCaseId { get; init; }

    public required IReadOnlyList<ClaimEvaluationResult> Claims { get; init; }

    public int TotalClaims => Claims.Count;

    public int SupportedClaims => Claims.Count(c => c.Outcome == ClaimSupportOutcome.Supported);

    public double FaithfulnessScore => TotalClaims == 0 ? 0 : (double)SupportedClaims / TotalClaims;
}