using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
public sealed class LlmClaimNormalizerTests
{
    [TestMethod]
    public async Task NormalizeAsync_EmptyResponse_ThrowsException()
    {
        // Arrange
        var chatClient = new FakeChatClient("[]");
        var normalizer = new LlmClaimNormalizer(chatClient);

        IReadOnlyList<string> claims =
        [
            "Enterprise RAG systems must enforce authorization."
        ];

        // Act and Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => normalizer.NormalizeAsync(claims));

        Assert.AreEqual("Claim normalization removed every input claim.", exception.Message);
    }

    [TestMethod]
    public async Task NormalizeAsync_InventedClaim_ThrowsException()
    {
        // Arrange
        var chatClient = new FakeChatClient("""["The demonstration application implements authorization."]""");

        var normalizer = new LlmClaimNormalizer(chatClient);

        IReadOnlyList<string> claims =
        [
            "Enterprise RAG systems must enforce authorization."
        ];

        // Act and Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => normalizer.NormalizeAsync(claims));

        Assert.AreEqual("Claim normalization returned a claim not present in the input.", exception.Message);
    }

    [TestMethod]
    public async Task NormalizeAsync_ValidSubset_ReturnsOriginalClaims()
    {
        // Arrange
        IReadOnlyList<string> claims =
        [
            "Enterprise RAG systems must enforce authorization.",
            "Enterprise RAG systems require authorization.",
            "OAuth 2.0 provides delegated authorization."
        ];

        var chatClient = new FakeChatClient(JsonSerializer.Serialize(new[]
            {
            claims[0],
            claims[2]
            }));

        var normalizer = new LlmClaimNormalizer(chatClient);

        // Act
        var normalized = await normalizer.NormalizeAsync(claims);

        // Assert
        Assert.AreEqual(2, normalized.Count);
        Assert.AreEqual(claims[0], normalized[0]);
        Assert.AreEqual(claims[2], normalized[1]);
    }
}

