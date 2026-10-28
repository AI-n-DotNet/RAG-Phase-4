using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

// Save this metadata with the vectors so benchmark inputs are identifiable.
public sealed record BenchmarkDataset(int SchemaVersion, string CorpusVersion, string EmbeddingModel, int Dimensions, IReadOnlyList<BenchmarkDatasetChunk> Chunks);

public sealed record BenchmarkDatasetChunk(int ChunkId, string Content, float[] Embedding);
