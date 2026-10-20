using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
public sealed class PassageClaimResolverTests
{
    [TestMethod]
    public void Resolve_ValidPassageId_PreservesTextAndCitations()
    {
        // Arrange
        const string answer = "The retrieved sources do not identify implemented controls.\n" + "Enterprise RAG requires authorization [S1].";

        var passages = CitedPassageExtractor.Extract(answer);

        var claim = new PassageClaim(Claim: "Enterprise RAG requires authorization.", PassageId: 2);

        // Act
        var result = PassageClaimResolver.Resolve(claim, passages);

        // Assert
        Assert.AreEqual("Enterprise RAG requires authorization.", result.Claim);

        Assert.AreEqual("Enterprise RAG requires authorization [S1].", result.OriginalText);

        CollectionAssert.AreEqual(new[] { "S1" }, result.CitationLabels.ToArray());
    }

    [TestMethod]
    public void Resolve_UnknownPassageId_ThrowsInvalidOperationException()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1].";

        var passages = CitedPassageExtractor.Extract(answer);

        var claim = new PassageClaim(Claim: "Enterprise RAG requires authorization.", PassageId: 99);

        // Act
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => PassageClaimResolver.Resolve(claim, passages));

        // Assert
        StringAssert.Contains(exception.Message, "Unknown passage ID: 99");
    }

    [TestMethod]
    public void Resolve_MultipleClaimsInOnePassage_CurrentlySharesAllCitations()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1], " + "and supports auditing [S2].";

        var passages = CitedPassageExtractor.Extract(answer);

        var authorizationClaim = new PassageClaim(Claim: "Enterprise RAG requires authorization.", PassageId: 1);

        // Act
        var resolved = PassageClaimResolver.Resolve(authorizationClaim, passages);

        // Assert: Documents current behavior, not desired behavior.
        //CollectionAssert.AreEqual(new[] { "S1", "S2" }, resolved.CitationLabels.ToArray());

        // Assert: Citation boundaries are preserved.
        CollectionAssert.AreEqual(new[] { "S1" }, resolved.CitationLabels.ToArray());
    }

    [TestMethod]
    public void Resolve_SeparateCitedAssertions_AssignsCorrectCitations()
    {
        // Arrange
        const string answer = "Enterprise RAG requires authorization [S1], " + "and supports auditing [S2].";

        var passages = CitedPassageExtractor.Extract(answer);

        var authorizationClaim = new PassageClaim(Claim: "Enterprise RAG requires authorization.", PassageId: 1);

        var auditingClaim = new PassageClaim(Claim: "Enterprise RAG supports auditing.", PassageId: 2);

        // Act
        var authorization = PassageClaimResolver.Resolve(authorizationClaim, passages);

        var auditing = PassageClaimResolver.Resolve(auditingClaim, passages);

        // Assert
        Assert.AreEqual(2, passages.Count);

        CollectionAssert.AreEqual(new[] { "S1" }, authorization.CitationLabels.ToArray());

        CollectionAssert.AreEqual(new[] { "S2" }, auditing.CitationLabels.ToArray());

        Assert.IsTrue(answer.Contains(authorization.OriginalText, StringComparison.Ordinal));

        Assert.IsTrue(answer.Contains(auditing.OriginalText, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Resolve_PreservesSemanticContextAndCitationBoundaries()
    {
        // Arrange
        string sentence = "The sources recommend authorization controls [S1], " + "but do not confirm their implementation.";

        var passages = CitedPassageExtractor.Extract(sentence);

        var claim = new PassageClaim("The sources do not confirm implementation.", passages[1].PassageId);

        // Act
        CitedClaim result = PassageClaimResolver.Resolve(claim, passages);

        // Assert
        Assert.AreEqual("but do not confirm their implementation.", result.OriginalText);

        Assert.AreEqual(sentence, result.ContextText);

        Assert.AreEqual(0, result.CitationLabels.Count);
    }
}