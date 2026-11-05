using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public sealed class RagEvaluator : IRagEvaluator
{
    private readonly IFaithfulnessEvaluator _faithfulnessEvaluator;
    private readonly ICitationVerifier _citationVerifier;

    public RagEvaluator(IFaithfulnessEvaluator faithfulnessEvaluator, ICitationVerifier citationVerifier)
    {
        _faithfulnessEvaluator = faithfulnessEvaluator;
        _citationVerifier = citationVerifier;
    }

    public async Task<RagEvaluationResult> EvaluateAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluationRun);

        cancellationToken.ThrowIfCancellationRequested();

        FaithfulnessEvaluationResult faithfulness = await _faithfulnessEvaluator.EvaluateAsync(evaluationRun, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        CitationVerificationResult citations = await _citationVerifier.VerifyAsync(evaluationRun, cancellationToken);

        if (faithfulness.EvaluationCaseId != evaluationRun.EvaluationCaseId)
        {
            throw new InvalidOperationException("Faithfulness evaluation case ID does not match the evaluation run.");
        }

        if (citations.EvaluationCaseId != evaluationRun.EvaluationCaseId)
        {
            throw new InvalidOperationException("Citation evaluation case ID does not match the evaluation run.");
        }

        return new RagEvaluationResult
        {
            EvaluationCaseId = evaluationRun.EvaluationCaseId,
            Faithfulness = faithfulness,
            Citations = citations
        };
    }
}