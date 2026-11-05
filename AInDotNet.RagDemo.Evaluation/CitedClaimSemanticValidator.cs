using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class CitedClaimSemanticValidator
{
    private readonly IClaimSpanVerifier _spanVerifier;

    public CitedClaimSemanticValidator(IClaimSpanVerifier spanVerifier)
    {
        _spanVerifier = spanVerifier;
    }

    public async Task<bool> ValidateAsync(CitedClaim citedClaim, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(citedClaim);

        if (string.IsNullOrWhiteSpace(citedClaim.OriginalText))
        {
            return false;
        }

        return await _spanVerifier.VerifyAsync(citedClaim.Claim, citedClaim.OriginalText, string.IsNullOrWhiteSpace(citedClaim.ContextText) ? citedClaim.OriginalText : citedClaim.ContextText, cancellationToken);
    }
}