using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface IFaithfulnessEvaluator
{
    Task<FaithfulnessEvaluationResult> EvaluateAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default);
}