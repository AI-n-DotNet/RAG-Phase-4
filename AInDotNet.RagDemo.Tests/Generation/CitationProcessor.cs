using AInDotNet.RagDemo.Application.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Generation;


[TestClass]
public sealed class CitationProcessorTests
{
    private static readonly RagSource[] Sources =
    [
        new("S1", 69, 1, "Enterprise RAG Architecture",            "Enterprise RAG must enforce authorization.", 0.37),

        new("S2", 49, 2, "OAuth Authorization",            "OAuth 2.0 provides delegated authorization.", 0.57)
    ];

    [TestMethod]
    public void Process_SingleCitation_ReturnsSource()
    {
        var result = CitationProcessor.Process("Authorization is required [S1].", Sources);

        Assert.AreEqual("Authorization is required [S1].", result.Answer);
        Assert.AreEqual(1, result.Sources.Count);
        Assert.AreEqual("S1", result.Sources[0].Label);
    }

    [TestMethod]
    public void Process_GroupedCitations_ReturnsBothSources()
    {
        var result = CitationProcessor.Process("Authorization mechanisms [S1, S2].", Sources);

        Assert.AreEqual("Authorization mechanisms [S1, S2].", result.Answer);

        Assert.AreEqual(2, result.Sources.Count);
    }

    [TestMethod]
    public void Process_AdjacentCitations_ReturnsBothSources()
    {
        var result = CitationProcessor.Process("Authorization mechanisms [S1][S2].", Sources);

        Assert.AreEqual("Authorization mechanisms [S1][S2].", result.Answer);

        Assert.AreEqual(2, result.Sources.Count);
    }

    [TestMethod]
    public void Process_DuplicateCitations_ReturnsSourceOnce()
    {
        var result = CitationProcessor.Process("First claim [S1]. Second claim [S1].", Sources);

        Assert.AreEqual(1, result.Sources.Count);
        Assert.AreEqual("S1", result.Sources[0].Label);
    }

    [TestMethod]
    public void Process_UnknownCitation_RemovesCitation()
    {
        var result = CitationProcessor.Process("Unsupported claim [S99].", Sources);

        Assert.AreEqual("Unsupported claim .", result.Answer);

        Assert.AreEqual(0, result.Sources.Count);
    }

    [TestMethod]
    public void Process_MixedValidAndUnknownCitations_KeepsValid()
    {
        var result = CitationProcessor.Process("Authorization [S1, S99].", Sources);

        Assert.AreEqual("Authorization [S1].", result.Answer);

        Assert.AreEqual(1, result.Sources.Count);
        Assert.AreEqual("S1", result.Sources[0].Label);
    }

    [TestMethod]
    public void Process_SourceIdentifierInProse_DoesNotCountAsCitation()
    {
        var result = CitationProcessor.Process("Source S1 discusses authorization.", Sources);

        Assert.AreEqual(0, result.Sources.Count);
    }

    [TestMethod]
    public void Process_NoCitations_ReturnsNoSources()
    {
        var result = CitationProcessor.Process("The available documentation is insufficient.", Sources);

        Assert.AreEqual(0, result.Sources.Count);
        Assert.AreEqual("The available documentation is insufficient.", result.Answer);
    }

    [TestMethod]
    public void Process_ReturnedSources_MustBeValidAndCited()
    {
        // Arrange
        const string answer = "Authorization is required [S1, S99]. OAuth provides delegated authorization [S2].";

        // Act
        var result = CitationProcessor.Process(answer, Sources);

        // Assert
        var validLabels = Sources.Select(source => source.Label).ToHashSet();

        foreach (var source in result.Sources)
        {
            // Returned source must exist in the supplied sources.
            Assert.IsTrue(validLabels.Contains(source.Label));

            // Returned source must appear in the normalized answer.
            Assert.IsTrue(result.Answer.Contains($"[{source.Label}]", StringComparison.Ordinal) ||
                result.Answer.Contains($"[{source.Label},", StringComparison.Ordinal) ||
                result.Answer.Contains($", {source.Label}]", StringComparison.Ordinal));
        }

        // Unknown citation must be removed.
        Assert.IsFalse(result.Answer.Contains("[S99]"));

        // Both valid sources must be returned.
        Assert.AreEqual(2, result.Sources.Count);
    }
}