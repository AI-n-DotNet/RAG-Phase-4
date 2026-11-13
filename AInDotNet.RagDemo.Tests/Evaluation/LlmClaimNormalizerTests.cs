using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AInDotNet.RagDemo.Tests.Evaluation;


[TestClass]
[TestCategory("Deterministic")]
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
    [TestMethod]
    public async Task NormalizeAsync_RecordedEvidenceDuplicates_PreservesRequirementAndExistingLimitation()
    {
        string[] claims =
        [
            "The retrieved sources do not establish which, if any, authorization controls are currently implemented in your RAG demonstration application.",
            "One source recommends that enterprise RAG systems must enforce authorization.",
            "No source explicitly confirms the actual implementation of authorization controls in the specific RAG demonstration application."
        ];
        var normalizer = new LlmClaimNormalizer(new FakeChatClient(
            JsonSerializer.Serialize(new[] { claims[0], claims[1] })));

        var normalized = await normalizer.NormalizeAsync(claims);

        CollectionAssert.AreEqual(new[] { claims[0], claims[1] }, normalized.ToArray());
        // This replay checks response handling, not live semantic deduplication.
        // The extraction-plus-normalization live test assesses that model decision.
    }
}

