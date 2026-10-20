using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Data.Models;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Application.Ingestion;

public sealed class DocumentIngestionService
{
    private readonly AInDotNetRAGContext _db;
    private readonly DocumentChunkingService _chunker;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public DocumentIngestionService(AInDotNetRAGContext db, DocumentChunkingService chunker, IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _db = db;
        _chunker = chunker;
        _generator = generator;
    }

    public async Task<DocumentIngestionResult> IngestAsync(string title, string sourceUri, string content, int maxChunkSize = 1500, int overlapSize = 200, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(sourceUri))
            throw new ArgumentException("Source URI is required.", nameof(sourceUri));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Document content is required.", nameof(content));

        // Check for an existing document.
        var existingDocument = await _db.Documents.AsNoTracking().Where(d => d.SourceUri == sourceUri)
            .Select(d => new
            {
                d.Id,
                ChunkCount = d.DocumentChunks.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingDocument is not null)
        {
            return new DocumentIngestionResult(existingDocument.Id, existingDocument.ChunkCount, WasInserted: false);
        }

        // Divide document text into overlapping chunks.
        var chunks = _chunker.ChunkDocument(content, maxChunkSize, overlapSize);

        var document = new Document
        {
            Title = title,
            SourceUri = sourceUri
        };

        // Generate and store one embedding per chunk.
        foreach (var chunk in chunks)
        {
            var embedding = await _generator.GenerateAsync(chunk.Content, cancellationToken: cancellationToken);

            if (embedding.Vector.Length != 1536)
            {
                throw new InvalidOperationException($"Expected 1536 embedding dimensions, " + $"received {embedding.Vector.Length}.");
            }

            document.DocumentChunks.Add(new DocumentChunk
            {
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                Embedding = new SqlVector<float>(embedding.Vector)
            });
        }

        // EF Core inserts the document and all related chunks.
        // SaveChangesAsync uses a transaction for these changes
        // when the provider supports transactions.
        _db.Documents.Add(document);

        await _db.SaveChangesAsync(cancellationToken);

        return new DocumentIngestionResult(document.Id, chunks.Count, WasInserted: true);
    }
}

public sealed record DocumentIngestionResult(int DocumentId, int ChunkCount, bool WasInserted);
