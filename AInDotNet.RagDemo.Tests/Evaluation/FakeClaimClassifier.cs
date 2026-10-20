using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FakeClaimClassifier : IClaimClassifier
{
    private readonly ClaimType _claimType;
    private readonly Dictionary<string, ClaimType>? _claimTypes;

    public FakeClaimClassifier(ClaimType claimType = ClaimType.Factual)
    {
        _claimType = claimType;
    }


    public FakeClaimClassifier(Dictionary<string, ClaimType> claimTypes)
    {
        _claimTypes = claimTypes;
    }

    public Task<ClaimType> ClassifyAsync(string claim, CancellationToken cancellationToken = default)
    {
        ClaimType claimType = _claimTypes is not null ? _claimTypes[claim] : _claimType;

        return Task.FromResult(claimType);
    }
}