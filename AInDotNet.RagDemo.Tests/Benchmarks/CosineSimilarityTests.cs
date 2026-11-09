using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Benchmarks;


[TestClass]
[TestCategory("Deterministic")]
public sealed class CosineSimilarityTests
{
    [TestMethod]
    public void Calculate_KnownDirections_ReturnsExpectedSimilarities()
    {
        float[] reference = [1, 0];

        Assert.AreEqual(1.0, CosineSimilarity.Calculate(reference, [2, 0]), 1e-12, "Vectors in the same direction have similarity 1.");

        Assert.AreEqual(0.0, CosineSimilarity.Calculate(reference, [0, 1]), 1e-12, "Perpendicular vectors have similarity 0.");

        Assert.AreEqual(-1.0, CosineSimilarity.Calculate(reference, [-1, 0]), 1e-12, "Vectors in opposite directions have similarity -1.");

        Assert.AreEqual(1.0 / Math.Sqrt(2), CosineSimilarity.Calculate(reference, [1, 1]), 1e-12, "A diagonal vector has the expected intermediate similarity.");
    }

    [TestMethod]
    public void Calculate_InvalidVectors_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CosineSimilarity.Calculate([], []));

        Assert.ThrowsExactly<ArgumentException>(() => CosineSimilarity.Calculate([1, 0], [1]));

        Assert.ThrowsExactly<ArgumentException>(() => CosineSimilarity.Calculate([0, 0], [1, 0]));

        Assert.ThrowsExactly<ArgumentException>(() => CosineSimilarity.Calculate([1, 0], [0, 0]));

        Assert.ThrowsExactly<ArgumentException>(() => CosineSimilarity.Calculate([float.NaN, 0], [1, 0]));

        Assert.ThrowsExactly<ArgumentException>(() => CosineSimilarity.Calculate([1, 0], [float.PositiveInfinity, 0]));
    }
}