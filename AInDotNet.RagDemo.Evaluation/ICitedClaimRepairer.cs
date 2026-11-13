namespace AInDotNet.RagDemo.Evaluation;

/// <summary>Repairs rejected extraction text without changing its source metadata.</summary>
public interface ICitedClaimRepairer
{
    /// <summary>
    /// Returns exactly one replacement per rejected claim, in the same order.
    /// Each replacement must preserve that assertion's attribution, scope and uncertainty.
    /// Retain all assertion-body words in order. A leading impersonal reporting frame
    /// may be replaced by the actual source, preserving reporting negation; otherwise insert
    /// qualifications. If that cannot faithfully repair the assertion, report failure.
    /// Citation markers, punctuation and letter casing do not count as claim words.
    /// </summary>
    Task<IReadOnlyList<string>> RepairAsync(
        IReadOnlyList<CitedClaim> rejectedClaims,
        CancellationToken cancellationToken = default);
}
