using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Benchmarks;

[TestClass]
public sealed class BenchmarkCorpusTests
{
    [TestMethod]
    public void Create_ReturnsUniqueReproduciblePassages()
    {
        var first = BenchmarkCorpus.Create();
        var second = BenchmarkCorpus.Create();

        Assert.AreEqual(1000, first.Count);

        Assert.AreEqual(1000, first.Distinct(StringComparer.Ordinal).Count(), "Every passage must have distinct content.");

        CollectionAssert.AreEqual(first.ToArray(), second.ToArray(), "Repeated generation must preserve content and ordering.");

        Assert.IsTrue(first.All(passage => passage.Contains("fictional benchmark data", StringComparison.Ordinal)));
    }
}