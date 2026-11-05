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

    public CitationVerifier(ICitedClaimExtractor claimExtractor, CitedClaimSemanticValidator semanticValidator, IClaimEvidenceVerifier evidenceVerifier, IClaimClassifier claimClassifier)
    {
        _claimExtractor = claimExtractor;
        _semanticValidator = semanticValidator;
        _evidenceVerifier = evidenceVerifier;
        _claimClassifier = claimClassifier;
    }

    public async Task<CitationVerificationResult> VerifyAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluationRun);

        IReadOnlyList<CitedClaim> citedClaims = await _claimExtractor.ExtractAsync(evaluationRun.Answer, cancellationToken);
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
            bool isSemanticallyValid = await _semanticValidator.ValidateAsync(citedClaim, cancellationToken);


            if (!isSemanticallyValid)
            {
                Console.WriteLine($"REJECTED CLAIM: {citedClaim.Claim}");
                Console.WriteLine($"ORIGINAL PASSAGE: {citedClaim.OriginalText}");
                throw new InvalidOperationException($"Cited claim does not match its original answer passage: {citedClaim.Claim}");
            }

            if (_claimClassifier is not null)
            {
                ClaimType claimType = await _claimClassifier.ClassifyAsync(citedClaim.Claim, cancellationToken);
                //Console.WriteLine($"CLAIM CLASSIFICATION: {claimType} | {citedClaim.Claim}");
                //Console.WriteLine($"ORIGINAL PASSAGE: {citedClaim.OriginalText}");
            }

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
}