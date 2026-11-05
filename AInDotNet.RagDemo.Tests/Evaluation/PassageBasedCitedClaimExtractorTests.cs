using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public class PassageBasedCitedClaimExtractorTests
{
    [TestMethod]
    public async Task ExtractAsync_ValidPassageClaim_ResolvesCitation()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1].";

        var fakeExtractor = new FakePassageClaimExtractor();

        var adapter = new PassageBasedCitedClaimExtractor(fakeExtractor);

        // Act
        var claims = await adapter.ExtractAsync(answer);

        // Assert
        Assert.AreEqual(1, claims.Count);

        Assert.AreEqual("Enterprise RAG requires authorization.", claims[0].Claim);

        Assert.AreEqual(answer, claims[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, claims[0].CitationLabels.ToArray());
    }

    private sealed class FakePassageClaimExtractor : IPassageClaimExtractor
    {
        public Task<IReadOnlyList<PassageClaim>> ExtractAsync(IReadOnlyList<CitedPassage> passages, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PassageClaim> claims =
            [
                new PassageClaim(Claim: passages[0].OriginalText.Replace(" [S1]", ""), PassageId: 1)
            ];

            return Task.FromResult(claims);
        }
    }

    [TestMethod]
    public async Task ExtractAsync_UncitedClaim_PreservesEmptyCitations()
    {
        // Arrange
        const string answer = "The application encrypts all documents.";

        var fakeExtractor = new FakePassageClaimExtractor();

        var adapter = new PassageBasedCitedClaimExtractor(fakeExtractor);

        // Act
        var claims = await adapter.ExtractAsync(answer);

        // Assert
        Assert.AreEqual(1, claims.Count);

        Assert.AreEqual(answer, claims[0].OriginalText);

        Assert.AreEqual(0, claims[0].CitationLabels.Count);
    }
}