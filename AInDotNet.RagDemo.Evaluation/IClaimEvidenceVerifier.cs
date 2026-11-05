using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Generation;

namespace AInDotNet.RagDemo.Evaluation;

public interface IClaimEvidenceVerifier
{
    Task<ClaimEvaluationResult> VerifyAsync(string claim, IReadOnlyList<RagSource> sources, CancellationToken cancellationToken = default);

    Task<ClaimEvaluationResult> VerifyAsync(string claim, IReadOnlyList<RagSource> sources, EvidenceScope evidenceScope, CancellationToken cancellationToken = default)
    {
        return VerifyAsync(claim, sources, cancellationToken);
    }
}
