using AInDotNet.RagDemo.Benchmarks;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Benchmarks;

[TestClass]
[TestCategory("Deterministic")]
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

    [TestMethod]
    public void CreateTenThousand_PreservesOriginalPrefixAndUniqueContent()
    {
        var original = BenchmarkCorpus.Create();
        var expanded = BenchmarkCorpus.CreateTenThousand();

        Assert.AreEqual(10000, expanded.Count);

        CollectionAssert.AreEqual(original.ToArray(), expanded.Take(original.Count).ToArray(), "Existing cached passages must remain unchanged.");

        Assert.AreEqual(10000, expanded.Distinct(StringComparer.Ordinal).Count());

        CollectionAssert.AreEqual(expanded.ToArray(), BenchmarkCorpus.CreateTenThousand().ToArray(), "The expanded corpus must remain reproducible.");
    }
}