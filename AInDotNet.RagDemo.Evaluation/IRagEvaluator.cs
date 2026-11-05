using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface IRagEvaluator
{
    Task<RagEvaluationResult> EvaluateAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default);
}