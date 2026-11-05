using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class ClaimEntailmentSpanVerifierAdapter : IClaimSpanVerifier
{
    private readonly IClaimEntailmentVerifier _entailmentVerifier;

    public ClaimEntailmentSpanVerifierAdapter(IClaimEntailmentVerifier entailmentVerifier)
    {
        _entailmentVerifier = entailmentVerifier;
    }

    public async Task<bool> VerifyAsync(string claim, string originalText, CancellationToken cancellationToken = default)
    {
        ClaimEntailmentOutcome outcome = await _entailmentVerifier.VerifyAsync(claim, originalText, cancellationToken);

        return outcome == ClaimEntailmentOutcome.Entailed;
    }

    public async Task<bool> VerifyAsync(string claim, string originalText, string contextText, CancellationToken cancellationToken = default)
    {
        ClaimEntailmentOutcome outcome = await _entailmentVerifier.VerifyAsync(claim, originalText, contextText, cancellationToken);

        return outcome == ClaimEntailmentOutcome.Entailed;
    }
}