using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface ICitationVerifier
{
    Task<CitationVerificationResult> VerifyAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default);
}