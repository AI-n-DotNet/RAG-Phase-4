using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FakeFaithfulnessEvaluator : IFaithfulnessEvaluator
{
    private readonly FaithfulnessEvaluationResult _result;

    public CancellationToken LastCancellationToken { get; private set; }

    public int CallCount { get; private set; }

    public Action? OnEvaluate { get; set; }

    public FakeFaithfulnessEvaluator(FaithfulnessEvaluationResult result)
    {
        _result = result;
    }

    public Task<FaithfulnessEvaluationResult> EvaluateAsync(RagEvaluationRun evaluationRun, CancellationToken cancellationToken = default)
    {
        CallCount++;
        OnEvaluate?.Invoke();
        LastCancellationToken = cancellationToken;

        return Task.FromResult(_result);
    }
}