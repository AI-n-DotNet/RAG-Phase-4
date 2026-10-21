using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Benchmarks;


[TestClass]
public sealed class InMemoryVectorSearchTests
{
    [TestMethod]
    public void Search_KnownVectors_ReturnsClosestMatchesInOrder()
    {
        BenchmarkVector[] vectors =
        [
            new(40, [-1, 0]),
            new(30, [0, 1]),
            new(20, [1, 1]),
            new(11, [2, 0]),
            new(10, [1, 0])
        ];

        var matches = InMemoryVectorSearch.Search(query: [1, 0], vectors: vectors, topK: 3);

        CollectionAssert.AreEqual(new[] { 10, 11, 20 }, matches.Select(match => match.ChunkId).ToArray());

        Assert.AreEqual(1.0, matches[0].Similarity, 1e-12);
        Assert.AreEqual(1.0, matches[1].Similarity, 1e-12);
        Assert.AreEqual(1.0 / Math.Sqrt(2), matches[2].Similarity, 1e-12);
    }
}