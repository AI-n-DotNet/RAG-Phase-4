using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FaithfulnessEvaluator : IFaithfulnessEvaluator
{
    private readonly IClaimExtractor _claimExtractor;
    private readonly IClaimNormalizer _claimNormalizer;
    private readonly IClaimEvidenceVerifier _evidenceVerifier;

    public FaithfulnessEvaluator(IClaimExtractor claimExtractor, IClaimNormalizer claimNormalizer, IClaimEvidenceVerifier evidenceVerifier)
    {
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

        Console.WriteLine($"RAW CLAIM COUNT: {claims.Count}");
        foreach (string claim in claims)
        {
            Console.WriteLine($"RAW CLAIM: {claim}");
        }

        Console.WriteLine($"NORMALIZED CLAIM COUNT: {normalizedClaims.Count}");
        foreach (string claim in normalizedClaims)
        {
            Console.WriteLine($"NORMALIZED CLAIM: {claim}");
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