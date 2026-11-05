using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Evaluation;

namespace AInDotNet.RagDemo.Tests.Evaluation;


public sealed class ClaimVerificationReferenceCase
{
    public required string Id { get; init; }

    public required string Claim { get; init; }

    public required IReadOnlyList<RagSource> Sources { get; init; }

    public required ClaimSupportOutcome ExpectedOutcome { get; init; }
}