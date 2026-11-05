using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
public class SentenceBoundaryDetectorTests
{
    [TestMethod]
    public void IsSentenceBoundary_Initialism_DoesNotSplit()
    {
        // Arrange
        const string text = "The U.S. Department of Energy approved the policy [S1].";

        int periodIndex = text.IndexOf("S. Department", StringComparison.Ordinal) + 1;

        // Act
        bool isBoundary = SentenceBoundaryDetector.IsSentenceBoundary(text, periodIndex);

        // Assert
        Assert.IsFalse(isBoundary);
    }

    [TestMethod]
    public void IsSentenceBoundary_TwoSentences_ReturnsTrue()
    {
        // Arrange
        const string text = "Authorization is required [S1]. " + "Auditing is supported [S2].";

        int periodIndex = text.IndexOf("[S1].", StringComparison.Ordinal) + 4;

        // Act
        bool isBoundary = SentenceBoundaryDetector.IsSentenceBoundary(text, periodIndex);

        // Assert
        Assert.IsTrue(isBoundary);
    }

    [TestMethod]
    public void IsSentenceBoundary_TitleAbbreviation_ReturnsFalse()
    {
        // Arrange
        const string text = "Dr. Smith approved the authorization policy [S1].";

        int periodIndex = text.IndexOf("Dr.", StringComparison.Ordinal) + 2;

        // Act
        bool isBoundary = SentenceBoundaryDetector.IsSentenceBoundary(text, periodIndex);

        // Assert
        Assert.IsFalse(isBoundary);
    }
}