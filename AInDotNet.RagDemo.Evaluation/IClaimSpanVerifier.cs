using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface IClaimSpanVerifier
{
    Task<bool> VerifyAsync(string claim, string originalText, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(string claim, string originalText, string contextText, CancellationToken cancellationToken = default)
        => VerifyAsync(claim, originalText, cancellationToken);
}