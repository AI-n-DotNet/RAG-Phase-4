using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class CitationVerifier : ICitationVerifier
{
    private readonly ICitedClaimExtractor _claimExtractor;
    private readonly CitedClaimSemanticValidator _semanticValidator;
    private readonly IClaimEvidenceVerifier _evidenceVerifier;
    private readonly IClaimClassifier _claimClassifier;

    private readonly Action<string>? _writeDiagnostic;
    private readonly ICitedClaimRepairer? _claimRepairer;

    public CitationVerifier(ICitedClaimExtractor claimExtractor, CitedClaimSemanticValidator semanticValidator, IClaimEvidenceVerifier evidenceVerifier, IClaimClassifier claimClassifier, Action<string>? writeDiagnostic = null, ICitedClaimRepairer? claimRepairer = null)
    {
        _writeDiagnostic = writeDiagnostic;
        _claimRepairer = claimRepairer;
        _claimExtractor = claimExtractor;
        _semanticValidator = semanticValidator;
        _evidenceVerifier = evidenceVerifier;
        _claimClassifier = claimClassifier;
    }

    public async Task<CitationVerificationResult> VerifyAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluationRun);

        var extractedClaims = await _claimExtractor.ExtractAsync(evaluationRun.Answer, cancellationToken);
        var citedClaims = await ValidateAndRepairClaimsAsync(extractedClaims, cancellationToken);
        int totalClaims = citedClaims.Count;
        int citedClaimsCount = citedClaims.Count(claim => claim.CitationLabels.Count > 0);

        int totalFactualClaims = 0;
        int citedFactualClaims = 0;

        foreach (CitedClaim claim in citedClaims)
        {
            ClaimType claimType = await _claimClassifier.ClassifyAsync(claim.Claim, cancellationToken);

            if (claimType == ClaimType.Factual)
            {
                totalFactualClaims++;

                if (claim.CitationLabels.Count > 0)
                {
                    citedFactualClaims++;
                }
            }
        }

        if (citedClaims.Count == 0)
        {
            return new CitationVerificationResult
            {
                EvaluationCaseId = evaluationRun.EvaluationCaseId,
                Citations = [],
                TotalClaims = totalClaims,
                CitedClaims = citedClaimsCount
            };
        }

        var citationResults = new List<CitationEvaluationResult>();

        foreach (CitedClaim citedClaim in citedClaims)
        {
            foreach (string citationLabel in citedClaim.CitationLabels)
            {
                // Each claim-citation pair will be evaluated independently.
                RagSource? source = evaluationRun.RetrievedSources.FirstOrDefault(s => string.Equals(s.Label, citationLabel, StringComparison.Ordinal));
                if (source is null)
                {
                    citationResults.Add(new CitationEvaluationResult
                    {
                        Claim = citedClaim.Claim,
                        SourceLabel = citationLabel,
                        Outcome = CitationVerificationOutcome.UnknownSource,
                        Explanation = "Citation label does not match any retrieved source."
                    });

                    continue;
                }

                //ClaimEvaluationResult evidenceResult = await _evidenceVerifier.VerifyAsync(citedClaim.Claim, new[] { source }, cancellationToken);
                ClaimEvaluationResult evidenceResult = await _evidenceVerifier.VerifyAsync(citedClaim.Claim, new[] { source }, EvidenceScope.IndividualSource, cancellationToken);

                CitationVerificationOutcome citationOutcome = evidenceResult.Outcome switch
                {
                    ClaimSupportOutcome.Supported => CitationVerificationOutcome.Supported,

                    ClaimSupportOutcome.Contradicted => CitationVerificationOutcome.Unsupported,

                    ClaimSupportOutcome.InsufficientEvidence => CitationVerificationOutcome.Unsupported,

                    _ => throw new InvalidOperationException($"Unexpected claim support outcome: {evidenceResult.Outcome}")
                };
                citationResults.Add(new CitationEvaluationResult
                {
                    Claim = citedClaim.Claim,
                    SourceLabel = citationLabel,
                    Outcome = citationOutcome,
                    Explanation = evidenceResult.Explanation
                });
            }
        }

        return new CitationVerificationResult
        {
            EvaluationCaseId = evaluationRun.EvaluationCaseId,
            Citations = citationResults,
            TotalClaims = totalClaims,
            CitedClaims = citedClaimsCount,
            TotalFactualClaims = totalFactualClaims,
            CitedFactualClaims = citedFactualClaims
        };
    }

    private async Task<IReadOnlyList<CitedClaim>> ValidateAndRepairClaimsAsync(
        IReadOnlyList<CitedClaim> extractedClaims, CancellationToken cancellationToken)
    {
        var claims = extractedClaims.ToArray();
        var rejectedIndices = new List<int>();

        for (int index = 0; index < claims.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _semanticValidator.ValidateAsync(claims[index], cancellationToken))
                continue;

            _writeDiagnostic?.Invoke($"REJECTED CLAIM: {claims[index].Claim}");
            _writeDiagnostic?.Invoke($"ORIGINAL PASSAGE: {claims[index].OriginalText}");

            if (_claimRepairer is null)
            {
                throw new InvalidOperationException(
                    $"Cited claim does not match its original answer passage: {claims[index].Claim}");
            }

            rejectedIndices.Add(index);
        }

        if (rejectedIndices.Count == 0)
            return claims;

        // One repair request for all rejected claims. Valid claims never enter repair.
        var rejectedClaims = rejectedIndices.Select(index => claims[index]).ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        _writeDiagnostic?.Invoke($"CLAIM REPAIR ATTEMPT: 1; rejected claims: {rejectedClaims.Length}");
        var repairedText = await _claimRepairer!.RepairAsync(rejectedClaims, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (repairedText is null || repairedText.Count != rejectedClaims.Length)
        {
            throw new InvalidOperationException(
                "Claim repair must return exactly one replacement per rejected claim.");
        }

        for (int index = 0; index < rejectedClaims.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(repairedText[index]))
                throw new InvalidOperationException("Claim repair returned an empty claim.");

            string claimText = CitationReferenceExtractor.RemoveMarkers(repairedText[index]);
            if (string.IsNullOrWhiteSpace(claimText))
                throw new InvalidOperationException("Claim repair returned no assertion.");

            var original = rejectedClaims[index];
            if (string.Equals(
                NormalizeClaimForComparison(original.Claim),
                NormalizeClaimForComparison(claimText),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Claim repair returned the rejected claim unchanged; no further attempt is allowed.");
            }
            var repaired = new CitedClaim
            {
                Claim = claimText,
                OriginalText = original.OriginalText,
                ContextText = original.ContextText,
                CitationLabels = original.CitationLabels
            };

            _writeDiagnostic?.Invoke($"REPAIRED CLAIM: {repaired.Claim}");
            if (!await _semanticValidator.ValidateAsync(repaired, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Cited claim remains invalid after one repair attempt. " +
                    $"Original extraction: {original.Claim} | Repaired claim: {repaired.Claim}");
            }

            claims[rejectedIndices[index]] = repaired;
        }

        // Classification and metrics run only after every claim has passed validation.
        return claims;
    }

    private static string NormalizeClaimForComparison(string text) =>
        string.Join(" ", CitationReferenceExtractor.RemoveMarkers(text)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}