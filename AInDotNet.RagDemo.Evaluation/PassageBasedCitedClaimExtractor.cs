using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class PassageBasedCitedClaimExtractor : ICitedClaimExtractor
{
    private readonly IPassageClaimExtractor _claimExtractor;

    public PassageBasedCitedClaimExtractor(IPassageClaimExtractor claimExtractor)
    {
        _claimExtractor = claimExtractor;
    }

    public async Task<IReadOnlyList<CitedClaim>> ExtractAsync(string answer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var passages = CitedPassageExtractor.Extract(answer);

        if (passages.Count == 0)
            return [];

        var claims = await _claimExtractor.ExtractAsync(passages, cancellationToken);

        return claims.Select(claim => PassageClaimResolver.Resolve(claim, passages)).ToArray();
    }
}