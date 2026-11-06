using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Evaluation;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class FaithfulnessEvaluator : IFaithfulnessEvaluator
{
    private readonly IClaimExtractor _claimExtractor;
    private readonly IClaimNormalizer _claimNormalizer;
    private readonly IClaimEvidenceVerifier _evidenceVerifier;

    private readonly Action<string>? _writeDiagnostic;

    public FaithfulnessEvaluator(IClaimExtractor claimExtractor, IClaimNormalizer claimNormalizer, IClaimEvidenceVerifier evidenceVerifier, Action<string>? writeDiagnostic = null)
    {
        _writeDiagnostic = writeDiagnostic;
        _claimExtractor = claimExtractor;
        _claimNormalizer = claimNormalizer;
        _evidenceVerifier = evidenceVerifier;
    }

    public async Task<FaithfulnessEvaluationResult> EvaluateAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluationRun);

        var claims = await _claimExtractor.ExtractClaimsAsync(evaluationRun.Answer, cancellationToken);
        var uniqueClaims = claims.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var normalizedClaims = await _claimNormalizer.NormalizeAsync(uniqueClaims, cancellationToken);

        _writeDiagnostic?.Invoke($"RAW CLAIM COUNT: {claims.Count}");
        foreach (string claim in claims)
        {
            _writeDiagnostic?.Invoke($"RAW CLAIM: {claim}");
        }

        _writeDiagnostic?.Invoke($"NORMALIZED CLAIM COUNT: {normalizedClaims.Count}");
        foreach (string claim in normalizedClaims)
        {
            _writeDiagnostic?.Invoke($"NORMALIZED CLAIM: {claim}");
        }

        var results = new List<ClaimEvaluationResult>();

        foreach (string claim in normalizedClaims)
        {
            var result = await _evidenceVerifier.VerifyAsync(claim, evaluationRun.RetrievedSources, cancellationToken);
            results.Add(result);
        }

        return new FaithfulnessEvaluationResult
        {
            EvaluationCaseId = evaluationRun.EvaluationCaseId,
            Claims = results
        };
    }
}