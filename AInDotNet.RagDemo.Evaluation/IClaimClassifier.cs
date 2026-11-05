using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public interface IClaimClassifier
{
    Task<ClaimType> ClassifyAsync(string claim, CancellationToken cancellationToken = default);
}