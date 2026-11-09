using AInDotNet.RagDemo.Evaluation;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class LlmPassageClaimExtractorContractTests
{
    [TestMethod]
    public async Task ExtractAsync_CitationMarkers_RemovesMarkersAndPreservesAttributionAndOwnership()
    {
        const string original =
            "They indicate that authorization is required [S1, S5].";
        IReadOnlyList<CitedPassage> passages =
        [
            new(PassageId: 7, OriginalText: original, CitationLabels: ["S1", "S5"])
        ];
        var extractor = CreateExtractor(
            "They indicate that authorization is required [S1, S5].", 7);

        var claims = await extractor.ExtractAsync(passages);

        Assert.AreEqual(1, claims.Count);
        Assert.AreEqual("They indicate that authorization is required.", claims[0].Claim);
        Assert.AreEqual(7, claims[0].PassageId);
        var resolved = PassageClaimResolver.Resolve(claims[0], passages);
        Assert.AreEqual(original, resolved.OriginalText);
        CollectionAssert.AreEqual(new[] { "S1", "S5" }, resolved.CitationLabels.ToArray());
    }

    [TestMethod]
    public async Task ExtractAsync_NoCitationMarkers_PreservesOtherBracketedText()
    {
        const string claim = "They recommend role-based access control [RBAC].";
        IReadOnlyList<CitedPassage> passages =
        [
            new(PassageId: 1, OriginalText: claim, CitationLabels: [])
        ];
        var extractor = CreateExtractor(claim, 1);

        var claims = await extractor.ExtractAsync(passages);

        Assert.AreEqual(claim, claims.Single().Claim);
    }

    [TestMethod]
    public async Task ExtractAsync_OnlyCitationMarkers_RejectsEmptyAssertion()
    {
        IReadOnlyList<CitedPassage> passages =
        [
            new(PassageId: 1, OriginalText: "Authorization is required [S1].", CitationLabels: ["S1"])
        ];
        var extractor = CreateExtractor("[S1]", 1);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => extractor.ExtractAsync(passages));

        Assert.AreEqual(
            "Passage claim extraction returned a claim containing only citation markers.",
            exception.Message);
    }

    private static LlmPassageClaimExtractor CreateExtractor(string claim, int passageId)
    {
        string response = JsonSerializer.Serialize(new[] { new { claim, passageId } });
        return new LlmPassageClaimExtractor(new FakeChatClient(response));
    }
}
