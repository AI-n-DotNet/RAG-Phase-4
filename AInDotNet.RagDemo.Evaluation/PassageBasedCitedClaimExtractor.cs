using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class PassageBasedCitedClaimExtractor : ICitedClaimExtractor
{
    private readonly IPassageClaimExtractor _claimExtractor;

    private readonly Action<string>? _writeDiagnostic;

    public PassageBasedCitedClaimExtractor(IPassageClaimExtractor claimExtractor, Action<string>? writeDiagnostic = null)
    {
        _writeDiagnostic = writeDiagnostic;
        _claimExtractor = claimExtractor;
    }

    public async Task<IReadOnlyList<CitedClaim>> ExtractAsync(string answer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var passages = CitedPassageExtractor.Extract(answer, _writeDiagnostic);

        if (passages.Count == 0)
            return [];

        var claims = await _claimExtractor.ExtractAsync(passages, cancellationToken);

        return claims.Select(claim => PassageClaimResolver.Resolve(claim, passages)).ToArray();
    }
}