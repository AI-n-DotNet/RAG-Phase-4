using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface IClaimExtractor
{
    Task<IReadOnlyList<string>> ExtractClaimsAsync(string answer, CancellationToken cancellationToken = default);
}