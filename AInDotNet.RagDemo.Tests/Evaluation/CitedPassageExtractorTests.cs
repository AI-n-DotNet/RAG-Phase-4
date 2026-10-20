using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
public sealed class CitedPassageExtractorTests
{
    [TestMethod]
    public void Extract_TwoLines_PreservesTextAndCitations()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1].\n" + "Retrieved documents must respect permissions [S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual(1, passages[0].PassageId);
        Assert.AreEqual("Enterprise RAG requires authorization [S1].", passages[0].OriginalText);
        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());

        Assert.AreEqual(2, passages[1].PassageId);
        Assert.AreEqual("Retrieved documents must respect permissions [S2].", passages[1].OriginalText);
        CollectionAssert.AreEqual(new[] { "S2" }, passages[1].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_MultipleCitationsInOnePassage_PreservesAllLabels()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1], " + "and supports auditing [S2, S3].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual(1, passages[0].PassageId);
        Assert.AreEqual(2, passages[1].PassageId);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());

        CollectionAssert.AreEqual(new[] { "S2", "S3" }, passages[1].CitationLabels.ToArray());

        Assert.IsTrue(answer.Contains(passages[0].OriginalText, StringComparison.Ordinal));

        Assert.IsTrue(answer.Contains(passages[1].OriginalText, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Extract_UncitedPassage_ReturnsEmptyCitationLabels()
    {
        // Arrange
        const string answer = "The retrieved sources do not identify the implemented authorization controls.";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(1, passages[0].PassageId);

        Assert.AreEqual(answer, passages[0].OriginalText);

        Assert.AreEqual(0, passages[0].CitationLabels.Count);
    }

    [TestMethod]
    public void Extract_MultipleCitedAssertions_SeparatesCitationGroups()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1], " + "and supports auditing [S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());

        CollectionAssert.AreEqual(new[] { "S2" }, passages[1].CitationLabels.ToArray());

        Assert.IsTrue(answer.Contains(passages[0].OriginalText, StringComparison.Ordinal));

        Assert.IsTrue(answer.Contains(passages[1].OriginalText, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Extract_TwoCitedSentencesOnOneLine_SeparatesCitations()
    {
        // Arrange
        const string answer = "Authorization is required [S1]. " + "Auditing is supported [S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual("Authorization is required [S1].", passages[0].OriginalText);

        Assert.AreEqual("Auditing is supported [S2].", passages[1].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());

        CollectionAssert.AreEqual(new[] { "S2" }, passages[1].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_AdjacentCitations_PreservesSinglePassage()
    {
        // Arrange
        const string answer = "Authorization is required [S1][S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(answer, passages[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1", "S2" }, passages[0].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_SpaceSeparatedCitations_PreservesSinglePassage()
    {
        // Arrange
        const string answer = "Authorization is required [S1] [S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(answer, passages[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1", "S2" }, passages[0].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_CitedThenUncitedSentence_SeparatesPassages()
    {
        // Arrange
        const string answer = "Authorization is required [S1]. " + "The application encrypts all documents.";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual("Authorization is required [S1].", passages[0].OriginalText);

        Assert.AreEqual("The application encrypts all documents.", passages[1].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());

        Assert.AreEqual(0, passages[1].CitationLabels.Count);
    }

    [TestMethod]
    public void Extract_UncitedThenCitedSentence_SeparatesPassages()
    {
        // Arrange
        const string answer = "The application encrypts all documents. " + "Authorization is required [S1].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual("The application encrypts all documents.", passages[0].OriginalText);

        Assert.AreEqual("Authorization is required [S1].", passages[1].OriginalText);

        Assert.AreEqual(0, passages[0].CitationLabels.Count);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[1].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_AbbreviationWithinSentence_PreservesSinglePassage()
    {
        // Arrange
        const string answer = "Dr. Smith approved the authorization policy [S1].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(answer, passages[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_SentenceStartingWithAbbreviation_PreservesCorrectBoundaries()
    {
        // Arrange
        const string answer = "Authorization is required [S1]. " + "Dr. Smith approved the policy [S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual("Authorization is required [S1].", passages[0].OriginalText);

        Assert.AreEqual("Dr. Smith approved the policy [S2].", passages[1].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());

        CollectionAssert.AreEqual(new[] { "S2" }, passages[1].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_InitialismWithinSentence_PreservesSinglePassage()
    {
        // Arrange
        const string answer = "The U.S. Department of Energy approved the policy [S1].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(answer, passages[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, passages[0].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_CommaSeparatedCitations_PreservesSinglePassage()
    {
        // Arrange
        const string answer = "Authorization is required [S1], [S2].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(answer, passages[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1", "S2" }, passages[0].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_CompoundAssertionWithTrailingCitations_PreservesCitationGroup()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization and " + "OAuth 2.0 supports delegated authorization [S1, S5].";

        // Act
        var passages = CitedPassageExtractor.Extract(answer);

        // Assert
        Assert.AreEqual(1, passages.Count);

        Assert.AreEqual(answer, passages[0].OriginalText);

        CollectionAssert.AreEqual(new[] { "S1", "S5" }, passages[0].CitationLabels.ToArray());
    }

    [TestMethod]
    public void Extract_CitationFollowedByBut_PreservesPassageContext()
    {
        string answer = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        var passages = CitedPassageExtractor.Extract(answer);

        Assert.AreEqual(2, passages.Count);

        Assert.AreEqual("The sources recommend authorization controls [S1]", passages[0].OriginalText);

        Assert.AreEqual("but do not confirm their implementation.", passages[1].OriginalText);

        Assert.AreEqual(1, passages[0].CitationLabels.Count);
        Assert.AreEqual("S1", passages[0].CitationLabels[0]);

        Assert.AreEqual(0, passages[1].CitationLabels.Count);

        Assert.AreEqual("The sources recommend authorization controls [S1], " + "but do not confirm their implementation.", passages[0].ContextText);

        Assert.AreEqual(passages[0].ContextText, passages[1].ContextText);
    }
}