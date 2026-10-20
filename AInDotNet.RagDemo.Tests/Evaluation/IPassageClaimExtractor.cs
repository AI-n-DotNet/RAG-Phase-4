using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public interface IPassageClaimExtractor
{
    Task<IReadOnlyList<PassageClaim>> ExtractAsync(IReadOnlyList<CitedPassage> passages, CancellationToken cancellationToken = default);
}