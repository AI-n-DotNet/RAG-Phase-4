using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class CitationReferenceExtractorTests
{
    [TestMethod]
    public void Extract_SingleCitation_ReturnsLabel()
    {
        var labels = CitationReferenceExtractor.Extract("Enterprise RAG requires authorization [S1].");

        CollectionAssert.AreEqual(new[] { "S1" }, labels.ToArray());
    }

    [TestMethod]
    public void Extract_MultipleCitations_ReturnsLabels()
    {
        var labels = CitationReferenceExtractor.Extract("Authorization and OAuth are discussed [S1, S5].");

        CollectionAssert.AreEqual(new[] { "S1", "S5" }, labels.ToArray());
    }

    [TestMethod]
    public void Extract_AdjacentCitations_ReturnsLabels()
    {
        var labels = CitationReferenceExtractor.Extract("Authorization is required [S1][S5].");

        CollectionAssert.AreEqual(new[] { "S1", "S5" }, labels.ToArray());
    }

    [TestMethod]
    public void Extract_DuplicateCitations_ReturnsDistinctLabels()
    {
        var labels = CitationReferenceExtractor.Extract("Authorization [S1]. OAuth [S5]. More authorization [S1].");

        CollectionAssert.AreEqual(new[] { "S1", "S5" }, labels.ToArray());
    }

    [TestMethod]
    public void Extract_NoCitations_ReturnsEmpty()
    {
        var labels = CitationReferenceExtractor.Extract("The retrieved sources do not establish authorization.");

        Assert.AreEqual(0, labels.Count);
    }
    [TestMethod]
    [DataRow("[Source S1][Source S5]")]
    [DataRow("[Source S1, S5]")]
    [DataRow("[Source S1, Source S5]")]
    [DataRow("[S1, Source S5]")]
    [DataRow("[source S1][SOURCE S5]")]
    public void Extract_SourcePrefixedMarkers_ReturnCanonicalLabels(string markers)
    {
        CollectionAssert.AreEqual(new[] { "S1", "S5" },
            CitationReferenceExtractor.Extract("Requirements " + markers).ToArray());
        Assert.AreEqual("Requirements.",
            CitationReferenceExtractor.RemoveMarkers("Requirements " + markers + "."));
    }

    [TestMethod]
    [DataRow("Source S1")]
    [DataRow("[Source S1 text]")]
    [DataRow("[Source S]")]
    [DataRow("[RBAC]")]
    public void Extract_ProseOrInvalidBracketText_IsNotCitation(string text)
    {
        Assert.AreEqual(0, CitationReferenceExtractor.Extract(text).Count);
        Assert.AreEqual(text, CitationReferenceExtractor.RemoveMarkers(text));
    }
}