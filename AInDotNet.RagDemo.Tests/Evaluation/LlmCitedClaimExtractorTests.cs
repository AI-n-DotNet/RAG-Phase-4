using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
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

    [TestMethod]
    public async Task ExtractAsync_UncitedClaimWithInventedOriginalText_ThrowsException()
    {
        const string answer = "Enterprise RAG requires authorization.";
        const string llmResponse = """
        [
            {
                "claim": "The application implements authorization.",
                "citationLabels": [],
                "originalText": "The application implements authorization."
            }
        ]
        """;

        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(llmResponse));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => extractor.ExtractAsync(answer));

        StringAssert.StartsWith(exception.Message,
            "Cited claim extraction returned original text not found in the answer.");
    }

    [TestMethod]
    public async Task ExtractAsync_OmittedCitationLabels_ThrowsException()
    {
        const string answer = "Enterprise RAG requires authorization [S1].";
        const string llmResponse = """
        [
            {
                "claim": "Enterprise RAG requires authorization.",
                "citationLabels": [],
                "originalText": "Enterprise RAG requires authorization [S1]."
            }
        ]
        """;

        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(llmResponse));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => extractor.ExtractAsync(answer));

        StringAssert.StartsWith(exception.Message,
            "Cited claim extraction returned citations that do not match the original text.");
    }

    [TestMethod]
    public async Task ExtractAsync_CollapsedLineBreak_RestoresOriginalAnswerText()
    {
        const string original = "The retrieved sources do not establish which controls\n" +
            "are implemented in our demonstration application.";
        const string answer = "Enterprise RAG requires authorization [S1].\n" + original;
        string response = System.Text.Json.JsonSerializer.Serialize(new[]
        {
            new
            {
                claim = "The retrieved sources do not establish which controls are implemented in our demonstration application.",
                citationLabels = Array.Empty<string>(),
                originalText = original.Replace("\n", " ")
            }
        });
        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(response));

        var claims = await extractor.ExtractAsync(answer);

        Assert.AreEqual(1, claims.Count);
        Assert.AreEqual(original, claims[0].OriginalText);
        Assert.IsTrue(answer.Contains(claims[0].OriginalText!, StringComparison.Ordinal));
        Assert.AreEqual(0, claims[0].CitationLabels.Count);
    }

    [TestMethod]
    public async Task ExtractAsync_ChangedWordsAcrossLineBreak_ThrowsException()
    {
        const string answer = "The sources do not confirm\nimplementation.";
        const string response = """
        [{
            "claim": "The sources confirm implementation.",
            "citationLabels": [],
            "originalText": "The sources confirm implementation."
        }]
        """;
        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(response));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => extractor.ExtractAsync(answer));

        StringAssert.StartsWith(exception.Message,
            "Cited claim extraction returned original text not found in the answer.");
    }

    [TestMethod]
    public async Task ExtractAsync_AmbiguousWhitespaceMatch_ThrowsException()
    {
        const string answer = "Authorization is\nrequired.\nAuthorization is  required.";
        const string response = """
        [{
            "claim": "Authorization is required.",
            "citationLabels": [],
            "originalText": "Authorization is required."
        }]
        """;
        var extractor = new LlmCitedClaimExtractor(new FakeChatClient(response));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => extractor.ExtractAsync(answer));

        StringAssert.StartsWith(exception.Message,
            "Cited claim extraction returned original text not found in the answer.");
    }
}