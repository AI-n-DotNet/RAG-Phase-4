using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FakeCitedClaimExtractor : ICitedClaimExtractor
{
    private readonly IReadOnlyList<CitedClaim> _claims;

    public FakeCitedClaimExtractor(IReadOnlyList<CitedClaim> claims)
    {
        _claims = claims;
    }

    public Task<IReadOnlyList<CitedClaim>> ExtractAsync(string answer, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_claims);
    }
}