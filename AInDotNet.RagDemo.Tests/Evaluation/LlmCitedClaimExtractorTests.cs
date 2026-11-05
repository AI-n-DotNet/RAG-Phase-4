using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
public sealed class LlmCitedClaimExtractorTests
{
    [TestMethod]
    public async Task ExtractAsync_InventedCitation_ThrowsException()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1].";

        const string llmResponse = """
            [
                {
                    "claim": "Enterprise RAG requires authorization.",
                    "citationLabels": ["S9"],
                    "originalText": "Enterprise RAG requires authorization [S1]."
                }
            ]
            """;

        var chatClient = new FakeChatClient(llmResponse);
        var extractor = new LlmCitedClaimExtractor(chatClient);

        // Act and Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => extractor.ExtractAsync(answer));

        Assert.AreEqual("Cited claim extraction returned an unknown citation label: S9", exception.Message);
    }

    [TestMethod]
    public async Task ExtractAsync_ValidCitations_ReturnsCitedClaims()
    {
        // Arrange
        const string answer = """
        Enterprise RAG requires authorization [S1].
        OAuth 2.0 provides delegated authorization [S5].
        """;

        const string llmResponse = """
        [
            {
                "claim": "Enterprise RAG requires authorization.",
                "citationLabels": ["S1"],
                "originalText": "Enterprise RAG requires authorization [S1]."
            },
            {
                "claim": "OAuth 2.0 provides delegated authorization.",
                "citationLabels": ["S5"],
                "originalText": "OAuth 2.0 provides delegated authorization [S5]."
            }
        ]
        """;

        var chatClient = new FakeChatClient(llmResponse);
        var extractor = new LlmCitedClaimExtractor(chatClient);

        // Act
        var results = await extractor.ExtractAsync(answer);

        // Assert
        Assert.AreEqual(2, results.Count);

        Assert.AreEqual("Enterprise RAG requires authorization.", results[0].Claim);

        CollectionAssert.AreEqual(new[] { "S1" }, results[0].CitationLabels.ToArray());

        Assert.AreEqual("OAuth 2.0 provides delegated authorization.", results[1].Claim);

        CollectionAssert.AreEqual(new[] { "S5" }, results[1].CitationLabels.ToArray());
    }

    [TestMethod]
    public async Task ExtractAsync_SwappedCitations_DetectsIncorrectAssociations()
    {
        // Arrange
        const string answer = """
        Enterprise RAG requires authorization [S1].
        OAuth 2.0 provides delegated authorization [S5].
        """;

        // The fake LLM incorrectly swaps the citations.
        const string llmResponse = """
        [
            {
                "claim": "Enterprise RAG requires authorization.",
                "citationLabels": ["S5"],
                "originalText": "Enterprise RAG requires authorization [S1]."
            },
            {
                "claim": "OAuth 2.0 provides delegated authorization.",
                "citationLabels": ["S1"],
                "originalText": "OAuth 2.0 provides delegated authorization [S5]."
            }
        ]
        """;

        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(llmResponse));

        // Act and Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => extractor.ExtractAsync(answer));

        //Assert.AreEqual("Cited claim extraction returned citations that do not match the original text.", exception.Message);
        StringAssert.StartsWith(exception.Message, "Cited claim extraction returned citations that do not match the original text.");
    }

    [TestMethod]
    public async Task ExtractAsync_WrongOriginalTextAssociation_ExposesLimitation()
    {
        const string answer = """
        Enterprise RAG requires authorization [S1].
        OAuth 2.0 provides delegated authorization [S5].
        """;

        const string llmResponse = """
        [
            {
                "claim": "Enterprise RAG requires authorization.",
                "citationLabels": ["S5"],
                "originalText": "OAuth 2.0 provides delegated authorization [S5]."
            }
        ]
        """;

        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(llmResponse));

        var results = await extractor.ExtractAsync(answer);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Enterprise RAG requires authorization.", results[0].Claim);

        CollectionAssert.AreEqual(new[] { "S5" }, results[0].CitationLabels.ToArray());
    }
}