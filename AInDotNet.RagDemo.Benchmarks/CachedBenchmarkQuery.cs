using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

public sealed record CachedBenchmarkQuery(int SchemaVersion, string EmbeddingModel, string Question, float[] Embedding);