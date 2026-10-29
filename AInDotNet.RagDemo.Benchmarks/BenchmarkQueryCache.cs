using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;


namespace AInDotNet.RagDemo.Benchmarks;

public static class BenchmarkQueryCache
{
    public static async Task<CachedBenchmarkQuery> GetOrCreateAsync(string path, string question, string embeddingModel, IEmbeddingGenerator<string, Embedding<float>> generator, CancellationToken cancellationToken = default)
    {
        path = Path.GetFullPath(path);

        CachedBenchmarkQuery saved;

        if (File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken);

            saved = JsonSerializer.Deserialize<CachedBenchmarkQuery>(json) ?? throw new InvalidOperationException($"Invalid question cache: {path}");
        }
        else
        {
            var embedding = await generator.GenerateAsync(question, cancellationToken: cancellationToken);

            saved = new CachedBenchmarkQuery(1, embeddingModel, question, embedding.Vector.ToArray());
        }

        if (saved.SchemaVersion != 1 || saved.EmbeddingModel != embeddingModel || saved.Question != question || saved.Embedding is null || saved.Embedding.Length != 1536 || saved.Embedding.Any(value => !float.IsFinite(value)) || !saved.Embedding.Any(value => value != 0))
        {
            throw new InvalidOperationException($"Question cache metadata or vector mismatch: {path}");
        }

        if (File.Exists(path))
        {
            Console.WriteLine($"Reusing {Path.GetFileName(path)}");
            return saved;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(saved), cancellationToken);

            File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        Console.WriteLine($"Generated {Path.GetFileName(path)}");

        return saved;
    }
}