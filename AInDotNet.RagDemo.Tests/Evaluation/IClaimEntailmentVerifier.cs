using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public interface IClaimEntailmentVerifier
{
    Task<ClaimEntailmentOutcome> VerifyAsync(string claim, string originalText, CancellationToken cancellationToken = default);
    Task<ClaimEntailmentOutcome> VerifyAsync(string claim, string originalText, string contextText, CancellationToken cancellationToken = default)
        => VerifyAsync(claim, originalText, cancellationToken);
}