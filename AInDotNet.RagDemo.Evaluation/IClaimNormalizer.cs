using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface IClaimNormalizer
{
    Task<IReadOnlyList<string>> NormalizeAsync(IReadOnlyList<string> claims, CancellationToken cancellationToken = default);
}
