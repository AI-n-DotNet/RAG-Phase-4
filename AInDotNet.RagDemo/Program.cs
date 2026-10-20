using AInDotNet.RagDemo.Application.Ingestion;
using AInDotNet.RagDemo.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using AInDotNet.RagDemo.Application.Retrieval;
using AInDotNet.RagDemo.Application.Generation;


// Reject unknown commands before configuring database or API access.
// No arguments retains the existing demonstration workflow.
if (args.Length > 0 && !string.Equals(args[0], "ingest", StringComparison.OrdinalIgnoreCase) && !string.Equals(args[0], "ask", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  ingest \"full path to document.txt\"");
    Console.Error.WriteLine("  ask \"your question\"");
    Console.Error.WriteLine("  No arguments: run the demonstration.");

    Environment.ExitCode = 1;
    return;
}

// Validate command arguments before loading configuration or services.
if (args.Length > 0 && (args.Length != 2 || string.IsNullOrWhiteSpace(args[1])))
{
    Console.Error.WriteLine(string.Equals(args[0], "ingest", StringComparison.OrdinalIgnoreCase) ? "Usage: ingest \"full path to document.txt\"" : "Usage: ask \"your question\"");

    Environment.ExitCode = 1;
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();


builder.Services.AddDbContext<AInDotNetRAGContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Read configuration
string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

string embeddingModel = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(apiKey).GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

builder.Services.AddScoped<RagDataSeeder>();

builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();

builder.Services.AddSingleton<DocumentChunkingService>();
builder.Services.AddScoped<DocumentIngestionService>();

string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

builder.Services.AddScoped<RagAnswerService>();

using var host = builder.Build();

// -----------------------   END OF SETUP -----------------------


using var scope = host.Services.CreateScope();

// see if we are loading a text file document from the command line
// To test this:
// In the console project’s debug launch settings, set command-line arguments to
// ingest "..\..\..\..\TestInputFiles\TestInputFile01.txt"
// Ingest a text file without running the demonstration questions.
if (args.Length > 0 && string.Equals(args[0], "ingest", StringComparison.OrdinalIgnoreCase))
{
    TextFileDocument document = await TextFileDocument.LoadAsync(args[1]);

    var ingestionService = scope.ServiceProvider.GetRequiredService<DocumentIngestionService>();

    DocumentIngestionResult ingestionResult = await ingestionService.IngestAsync(document.Title, document.SourceUri, document.Content);

    Console.WriteLine($"Document: {document.Title}");
    Console.WriteLine($"Document ID: {ingestionResult.DocumentId}");
    Console.WriteLine($"Chunks: {ingestionResult.ChunkCount}");
    Console.WriteLine(ingestionResult.WasInserted ? "Document ingested." : "Document already exists; ingestion skipped.");

    return;
}

// Ask command
// Answer a question using documents already stored in the database.
// To test this:
// In the console project’s debug launch settings, set command-line arguments to
// ask "Where does the AInDotNet demonstration store document embeddings?"
if (args.Length > 0 && string.Equals(args[0], "ask", StringComparison.OrdinalIgnoreCase))
{
    var answerService = scope.ServiceProvider.GetRequiredService<RagAnswerService>();

    RagAnswerResult answerResult = await answerService.AnswerAsync(args[1], topK: 5);

    Console.WriteLine($"QUESTION: {args[1]}");
    Console.WriteLine();
    Console.WriteLine("ANSWER:");
    Console.WriteLine(answerResult.Answer);
    Console.WriteLine();
    Console.WriteLine("CITED SOURCES:");

    foreach (var source in answerResult.Sources)
    {
        Console.WriteLine(
            $"[{source.Label}] {source.DocumentTitle} " +
            $"(Document {source.DocumentId}, " +
            $"Chunk {source.ChunkId}, " +
            $"Distance {source.Distance:F6})");
    }

    return;
}

using var db = scope.ServiceProvider.GetRequiredService<AInDotNetRAGContext>();
var seeder = scope.ServiceProvider.GetRequiredService<RagDataSeeder>();

await seeder.SeedAsync();

var searchService = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();

var ragService = scope.ServiceProvider.GetRequiredService<RagAnswerService>();
string[] questions =
[
    "How does enterprise RAG prevent unauthorized access?",
    "What security controls should enterprise RAG systems implement?",
    "Does the retrieved documentation establish that authorization controls are currently implemented?",
    "What authorization controls are currently implemented in our RAG demonstration application?"
];

RagAnswerResult result;

foreach (string question in questions)
{
    result = await ragService.AnswerAsync(question, topK: 5);
    Console.WriteLine($"QUESTION: {question}");
    Console.WriteLine();
    Console.WriteLine("ANSWER:");
    Console.WriteLine(result.Answer);
    Console.WriteLine();
    Console.WriteLine("CITED SOURCES:");
    foreach (var source in result.Sources)
    {
        Console.WriteLine(
            $"[{source.Label}] " +
            $"{source.DocumentTitle} " +
            $"(Chunk {source.ChunkId}, " +
            $"Distance {source.Distance:F6})");
    }
    Console.WriteLine(new string('-', 75));
}

