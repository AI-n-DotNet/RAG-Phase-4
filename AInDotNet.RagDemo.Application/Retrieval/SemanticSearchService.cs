using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Data.Models;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Application.Retrieval;

public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly AInDotNetRAGContext _db;

    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public SemanticSearchService(AInDotNetRAGContext db, IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _db = db;
        _generator = generator;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty.", nameof(query));

        if (topK <= 0)
            throw new ArgumentOutOfRangeException(nameof(topK));

        // Generate the query embedding.
        var embedding = await _generator.GenerateAsync(query, cancellationToken: cancellationToken);

        if (embedding.Vector.Length != 1536)
        {
            throw new InvalidOperationException($"Expected 1536 dimensions, received " + $"{embedding.Vector.Length}.");
        }

        var queryVector = new SqlVector<float>(embedding.Vector);

        // Execute similarity search inside SQL Server.
        var matches = await _db.DocumentChunks.AsNoTracking().Select(chunk => new
        {
            chunk.Id,
            chunk.DocumentId,
            DocumentTitle = chunk.Document.Title,
            chunk.Content,
            Distance = EF.Functions.VectorDistance(
                    "cosine",
                    chunk.Embedding,
                    queryVector)
        })
            .OrderBy(result => result.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        return matches.Select(match => new SearchResult(
                match.Id,
                match.DocumentId,
                match.DocumentTitle,
                match.Content,
                match.Distance))
            .ToList();
    }
}