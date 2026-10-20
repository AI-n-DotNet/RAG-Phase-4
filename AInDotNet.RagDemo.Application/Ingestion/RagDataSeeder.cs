using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Data;
using AInDotNet.RagDemo.Data.Models;
using global::AInDotNet.RagDemo.Data.Models;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Application.Ingestion;



public sealed class RagDataSeeder
{
    private readonly AInDotNetRAGContext _db;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public RagDataSeeder(AInDotNetRAGContext db, IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _db = db;
        _generator = generator;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var samples = GetSamples();

        int inserted = 0;
        int skipped = 0;

        foreach (var sample in samples)
        {
            string sourceUri = $"seed://aindotnet/{sample.Id}";

            bool exists = await _db.Documents.AsNoTracking().AnyAsync(d => d.SourceUri == sourceUri, cancellationToken);

            if (exists)
            {
                skipped++;
                continue;
            }

            var embedding = await _generator.GenerateAsync(sample.Content, cancellationToken: cancellationToken);

            if (embedding.Vector.Length != 1536)
            {
                throw new InvalidOperationException($"Expected 1536 dimensions, got " + $"{embedding.Vector.Length}.");
            }

            var document = new Document
            {
                Title = sample.Title,
                SourceUri = sourceUri
            };

            var chunk = new DocumentChunk
            {
                ChunkIndex = 0,
                Content = sample.Content,
                Embedding = new SqlVector<float>(embedding.Vector)
            };

            document.DocumentChunks.Add(chunk);

            _db.Documents.Add(document);

            await _db.SaveChangesAsync(cancellationToken);

            inserted++;

            Console.WriteLine($"Inserted: {sample.Title}");
        }

        Console.WriteLine($"\nSeeding complete. " + $"Inserted: {inserted}, Skipped: {skipped}");
    }

    private static IReadOnlyList<SeedDocument> GetSamples()
    {
        return
        [
            // SQL Server
            new("sql-01", "SQL Server Vector Search",                "SQL Server 2025 supports native vector " +                "storage and cosine similarity search."),

            new("sql-02", "SQL Server Indexing",                "Database indexes improve query performance " +                "by reducing unnecessary data access."),

            new("sql-03", "Relational Databases",                "Relational databases organize information " +                "into tables connected by primary and foreign keys."),

            new("sql-04", "SQL Server Transactions",                "SQL Server transactions provide atomicity, " +                "consistency, isolation, and durability."),

            new("sql-05", "SQL Server Query Optimization",                "SQL Server uses execution plans and statistics " +                "to optimize database queries."),

            // Entity Framework
            new("ef-01", "Entity Framework Core",                "Entity Framework Core is an object relational " +                "mapper for .NET applications."),

            new("ef-02", "LINQ",                "LINQ expressions can be translated into SQL " +                "and executed by the database server."),

            new("ef-03", "Database First Development",                "EF Core Power Tools can reverse engineer " +                "database tables into C# entity classes."),

            new("ef-04", "EF Core Change Tracking",                "Entity Framework Core tracks entity changes " +                "and persists updates through SaveChanges."),

            new("ef-05", "EF Core Relationships",                "Entity Framework Core maps navigation properties " +                "to relational database relationships."),

            // Retrieval Augmented Generation
            new("rag-01", "Retrieval Augmented Generation",                "RAG retrieves relevant external information " +                "and supplies it as context to a language model."),

            new("rag-02", "Document Chunking",                "Document chunking divides large documents " +                "into smaller passages for retrieval."),

            new("rag-03", "Embeddings",                "Embedding models convert text into numerical " +                "vectors representing semantic information."),

            new("rag-04", "Semantic Search",                "Semantic search retrieves documents based on " +                "meaning rather than exact keyword matching."),

            new("rag-05", "RAG Grounding",                "Grounding supplies retrieved source material " +                "to help an AI model produce evidence-based answers."),

            // Security
            new("sec-01", "OAuth Authorization",                "OAuth 2.0 provides a framework for delegated " +                "authorization to protected resources."),

            new("sec-02", "OpenID Connect",                "OpenID Connect adds an authentication identity " +                "layer on top of OAuth 2.0."),

            new("sec-03", "Role Based Access Control",                "RBAC assigns permissions to roles rather than " +                "individually to every user."),

            new("sec-04", "Zero Trust Security",                "Zero Trust security requires explicit verification " +                "rather than trusting network location."),

            new("sec-05", "Data Encryption",                "Encryption protects information by transforming " +                "plaintext into ciphertext using cryptographic keys."),

            // Healthcare
            new("health-01", "Electronic Health Records",                "Electronic health records store patient diagnoses, " +                "medications, laboratory results, and clinical notes."),

            new("health-02", "Healthcare Claims",                "Healthcare claims describe medical services " +                "submitted to insurers for reimbursement."),

            new("health-03", "Medical Coding",                "Medical coding maps diagnoses and procedures " +                "to standardized classification codes."),

            new("health-04", "Clinical Documentation",                "Clinical documentation records patient assessments, " +                "treatments, and care plans."),

            new("health-05", "Healthcare Interoperability",                "FHIR provides standards for exchanging " +                "healthcare information between systems."),

            // Forestry
            new("forest-01", "Forest Management",                "Forest management includes thinning, regeneration, " +                "habitat protection, and timber harvesting."),

            new("forest-02", "Forest Regeneration",                "Natural forest regeneration occurs when new trees " +                "establish from seeds or existing root systems."),

            new("forest-03", "Invasive Species",                "Invasive plant species can compete with native " +                "vegetation and alter forest ecosystems."),

            new("forest-04", "Watershed Protection",                "Forested watersheds help regulate runoff, " +                "reduce erosion, and protect water quality."),

            new("forest-05", "Selective Thinning",                "Selective thinning removes chosen trees " +                "to reduce competition and improve stand health.")
        ];
    }

    private sealed record SeedDocument(string Id, string Title, string Content);
}