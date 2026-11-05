using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface ICitedClaimExtractor
{
    Task<IReadOnlyList<CitedClaim>> ExtractAsync(string answer, CancellationToken cancellationToken = default);
}