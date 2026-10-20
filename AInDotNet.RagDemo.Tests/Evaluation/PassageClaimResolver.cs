using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public static class PassageClaimResolver
{
    public static CitedClaim Resolve(PassageClaim claim, IReadOnlyList<CitedPassage> passages)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(passages);

        var passage = passages.SingleOrDefault(p => p.PassageId == claim.PassageId);

        if (passage is null)
        {
            throw new InvalidOperationException($"Unknown passage ID: {claim.PassageId}");
        }

        return new CitedClaim
        {
            Claim = claim.Claim,
            OriginalText = passage.OriginalText,
            ContextText = passage.ContextText,
            CitationLabels = passage.CitationLabels
        };
    }
}