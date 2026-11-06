using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FakeClaimEvidenceVerifier : IClaimEvidenceVerifier
{
    public List<EvidenceScope> ReceivedScopes { get; } = [];

    private readonly ClaimSupportOutcome _outcome;
    private readonly Dictionary<string, ClaimSupportOutcome>? _outcomesBySource;

    public FakeClaimEvidenceVerifier(ClaimSupportOutcome outcome)
    {
        _outcome = outcome;
    }

    public FakeClaimEvidenceVerifier(Dictionary<string, ClaimSupportOutcome> outcomesBySource)
    {
        _outcomesBySource = outcomesBySource;
    }

    public Task<ClaimEvaluationResult> VerifyAsync(string claim, IReadOnlyList<RagSource> sources, EvidenceScope evidenceScope, CancellationToken cancellationToken = default)
    {
        ReceivedScopes.Add(evidenceScope);

        ClaimSupportOutcome outcome = _outcomesBySource is not null ? _outcomesBySource[sources.Single().Label] : _outcome;

        return Task.FromResult(new ClaimEvaluationResult
        {
            Claim = claim,
            Outcome = outcome,
            SupportingSourceLabels = outcome == ClaimSupportOutcome.Supported ? sources.Select(s => s.Label).ToList() : [],
            Explanation = $"Simulated evidence outcome: {outcome}"
        });
    }
}