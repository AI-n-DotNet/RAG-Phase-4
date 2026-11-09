namespace AInDotNet.RagDemo.Evaluation;

/// <summary>Repairs rejected extraction text without changing its source metadata.</summary>
public interface ICitedClaimRepairer
{
    /// <summary>
    /// Returns exactly one replacement per rejected claim, in the same order.
    /// Each replacement must preserve that assertion's attribution, scope and uncertainty.
    /// </summary>
    Task<IReadOnlyList<string>> RepairAsync(
        IReadOnlyList<CitedClaim> rejectedClaims,
        CancellationToken cancellationToken = default);
}
