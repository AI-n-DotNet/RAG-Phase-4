using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Benchmarks;


[TestClass]
public sealed class PreparedVectorSearchTests
{
    [TestMethod]
    public void Search_PreparedCache_MatchesOriginalCalculation()
    {
        BenchmarkVector[] vectors =
        [
            new(40, [-1, -2, -3]),
            new(30, [3, -1, 2]),
            new(20, [1, 1, 0]),
            new(11, [2, 4, 6]),
            new(10, [1, 2, 3])
        ];

        var cache = new PreparedVectorCache(vectors);

        float[][] queries =
        [
            [1, 2, 3],
            [-2, 1, 4]
        ];

        foreach (var query in queries)
        {
            var expected = InMemoryVectorSearch.Search(query, vectors, topK: 5);

            var actual = PreparedVectorSearch.Search(query, cache, topK: 5);

            CollectionAssert.AreEqual(expected.Select(match => match.ChunkId).ToArray(), actual.Select(match => match.ChunkId).ToArray());

            for (int index = 0; index < expected.Count; index++)
            {
                Assert.AreEqual(expected[index].Similarity, actual[index].Similarity, 1e-12);
            }
        }
    }
}