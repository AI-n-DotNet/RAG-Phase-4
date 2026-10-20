using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public enum CitationVerificationOutcome
{
    Supported,
    Unsupported,
    UnknownSource
}

public sealed class CitationEvaluationResult
{
    public required string Claim { get; init; }

    public required string SourceLabel { get; init; }

    public required CitationVerificationOutcome Outcome { get; init; }

    public string? Explanation { get; init; }
}

public sealed class CitationVerificationResult
{
    public required string EvaluationCaseId { get; init; }

    public required IReadOnlyList<CitationEvaluationResult> Citations
    {
        get;
        init;
    }

    public int TotalCitations => Citations.Count;

    public int SupportedCitations => Citations.Count(c => c.Outcome == CitationVerificationOutcome.Supported);

    public int UnsupportedCitations => Citations.Count(c => c.Outcome == CitationVerificationOutcome.Unsupported);

    public int UnknownSourceCitations => Citations.Count(c => c.Outcome == CitationVerificationOutcome.UnknownSource);

    public double CitationAccuracy => TotalCitations == 0 ? 0 : (double)SupportedCitations / TotalCitations;

    public int TotalClaims { get; init; }

    public int CitedClaims { get; init; }

    public double CitationCoverage => TotalClaims == 0 ? 0 : (double)CitedClaims / TotalClaims;

    public double CitedClaimSupportRate
    {
        get
        {
            var citedClaims = Citations.GroupBy(c => c.Claim, StringComparer.Ordinal).ToList();

            if (citedClaims.Count == 0)
                return 0;

            int supportedClaims = citedClaims.Count(group => group.Any(c => c.Outcome == CitationVerificationOutcome.Supported));

            return (double)supportedClaims / citedClaims.Count;
        }
    }

    public int TotalFactualClaims { get; init; }

    public int CitedFactualClaims { get; init; }

    public double FactualCitationCoverage => TotalFactualClaims == 0 ? 0 : (double)CitedFactualClaims / TotalFactualClaims;
}