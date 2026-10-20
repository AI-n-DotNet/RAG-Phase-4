using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FakeCitationVerifier : ICitationVerifier
{
    private readonly CitationVerificationResult _result;

    public CancellationToken LastCancellationToken { get; private set; }

    public int CallCount { get; private set; }

    public FakeCitationVerifier(CitationVerificationResult result)
    {
        _result = result;
    }

    public Task<CitationVerificationResult> VerifyAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastCancellationToken = cancellationToken;

        return Task.FromResult(_result);
    }
}