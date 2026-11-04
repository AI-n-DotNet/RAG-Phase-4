using AInDotNet.RagDemo.BenchmarkData.Models;
using AInDotNet.RagDemo.Benchmarks;
using AInDotNet.RagDemo.Tests.Benchmarks;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AInDotNet.RagDemo.ExpressData.Models;
//using DeveloperModels = AInDotNet.RagDemo.BenchmarkData.Models;

string expressConnection;

var builder = Host.CreateApplicationBuilder(args);

// Avoid per-query logging during timing.
builder.Logging.ClearProviders();

builder.Configuration.AddUserSecrets<Program>();

string? connectionString = builder.Configuration.GetConnectionString("BenchmarkConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Database connection string is missing. " + "Set ConnectionStrings:BenchmarkConnection in User Secrets.");
    Environment.ExitCode = 1;
    return;
}
builder.Services.AddDbContext<AInDotNetRAG_BenchmarkContext>(options => options.UseSqlServer(connectionString));


// Read configuration
string? apiKey = builder.Configuration["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("OpenAI API key is missing. " + "Set OpenAI:ApiKey in User Secrets.");
    Environment.ExitCode = 1;
    return;
}

string embeddingModel = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

// Generate each embedding once, then store the same values in both native-vector and JSON form.
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(apiKey).GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

using var host = builder.Build();

// -----------------------   END OF SETUP -----------------------


using var scope = host.Services.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<AInDotNetRAG_BenchmarkContext>();

var generator = scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

//await BenchmarkDatasetLoader.LoadSmallDatasetAsync(db, generator);

if (args.Length == 2 && (string.Equals(args[0], "import-data", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "import-10k", StringComparison.OrdinalIgnoreCase)))
{
    int chunkCount = string.Equals(args[0], "import-10k", StringComparison.OrdinalIgnoreCase) ? 10000 : 1000;

    BenchmarkDataset dataset = await BenchmarkDatasetReader.LoadAsync(Path.GetFullPath(args[1]), embeddingModel, chunkCount);
    await BenchmarkDatasetImporter.ImportAsync(db, dataset);

    return;
}

if (args.Length == 2 && (string.Equals(args[0], "inspect-data", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "inspect-10k", StringComparison.OrdinalIgnoreCase)))
{
    int chunkCount = string.Equals(args[0], "inspect-10k", StringComparison.OrdinalIgnoreCase) ? 10000 : 1000;

    BenchmarkDataset dataset = await BenchmarkDatasetReader.LoadAsync(Path.GetFullPath(args[1]), embeddingModel, chunkCount);

    Console.WriteLine($"Corpus: {dataset.CorpusVersion}");
    Console.WriteLine($"Model: {dataset.EmbeddingModel}");
    Console.WriteLine($"Dimensions: {dataset.Dimensions}");
    Console.WriteLine($"Validated chunks: {dataset.Chunks.Count}");

    return;
}

if (args.Length == 2 && (string.Equals(args[0], "prepare-preview", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "prepare-data", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "prepare-10k", StringComparison.OrdinalIgnoreCase)))
{
    IReadOnlyList<string> passages = string.Equals(args[0], "prepare-10k", StringComparison.OrdinalIgnoreCase) ? BenchmarkCorpus.CreateTenThousand()
        : string.Equals(args[0], "prepare-preview", StringComparison.OrdinalIgnoreCase) ? BenchmarkCorpus.Create().Take(3).ToArray() : BenchmarkCorpus.Create();

    BenchmarkDataset dataset = await BenchmarkDatasetPreparer.PrepareAsync(directory: Path.GetFullPath(args[1]), passages: passages, embeddingModel: embeddingModel, generator: generator);

    Console.WriteLine($"Prepared {dataset.Chunks.Count} cached embeddings.");

    return;
}

if (args.Length == 2 && (string.Equals(args[0], "import-express", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "import-express-10k", StringComparison.OrdinalIgnoreCase)))
{
    expressConnection = builder.Configuration.GetConnectionString("ExpressConnection") ?? throw new InvalidOperationException("ExpressConnection is missing.");

    var options = new DbContextOptionsBuilder<AInDotNetRAG_ExpressContext>().UseSqlServer(expressConnection).Options;

    await using var expressDbImport = new AInDotNetRAG_ExpressContext(options);

    string edition = await expressDbImport.Database.SqlQueryRaw<string>(
            """
            SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128))
                AS [Value]
            """)
        .SingleAsync();

    if (!edition.Contains("Express", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Expected Express; connected to {edition}.");
    }

    int chunkCount = string.Equals(args[0], "import-express-10k", StringComparison.OrdinalIgnoreCase) ? 10000 : 1000;

    BenchmarkDataset dataset = await BenchmarkDatasetReader.LoadAsync(Path.GetFullPath(args[1]), embeddingModel, chunkCount);
    await ExpressDatasetImporter.ImportAsync(expressDbImport, dataset);

    return;
}


if (args.Length == 1 && string.Equals(args[0], "list-questions", StringComparison.OrdinalIgnoreCase))
{
    foreach (var item in BenchmarkQuestions.All)
        Console.WriteLine($"{item.CacheName}: {item.Text}");

    Console.WriteLine($"Questions: {BenchmarkQuestions.All.Count}");
    return;
}

if (args.Length == 2 && string.Equals(args[0], "prepare-questions", StringComparison.OrdinalIgnoreCase))
{
    string directory = Path.GetFullPath(args[1]);

    foreach (var item in BenchmarkQuestions.All)
    {
        await BenchmarkQueryCache.GetOrCreateAsync(Path.Combine(directory, item.CacheName + ".json"), item.Text, embeddingModel, generator);
    }

    Console.WriteLine($"Prepared {BenchmarkQuestions.All.Count} question embeddings.");

    return;
}

if (args.Length == 2 && string.Equals(args[0], "verify-questions", StringComparison.OrdinalIgnoreCase))
{
    string verificationConnection = builder.Configuration.GetConnectionString("ExpressConnection") ?? throw new InvalidOperationException("ExpressConnection is missing.");

    var verificationOptions = new DbContextOptionsBuilder<AInDotNet.RagDemo.ExpressData.Models.AInDotNetRAG_ExpressContext>().UseSqlServer(verificationConnection).Options;

    await using var verificationDb = new AInDotNet.RagDemo.ExpressData.Models.AInDotNetRAG_ExpressContext(verificationOptions);

    await BenchmarkQuestionVerifier.VerifyAsync(db, verificationDb, Path.GetFullPath(args[1]), embeddingModel);

    return;
}

if (args.Length > 0 && string.Equals(args[0], "benchmark-mixed", StringComparison.OrdinalIgnoreCase))
{
    int workers = 16;

    if ((args.Length != 2 && args.Length != 3) || string.IsNullOrWhiteSpace(args[1]) || (args.Length == 3 && (!int.TryParse(args[2], out workers) || workers is not (4 or 8 or 16))))
    {
        Console.Error.WriteLine("Usage: benchmark-mixed \"query-cache-directory\" [4|8|16]");
        Environment.ExitCode = 1;
        return;
    }

    string mixedExpressConnection = builder.Configuration.GetConnectionString("ExpressConnection") ?? throw new InvalidOperationException("ExpressConnection is missing.");

    await MixedQuestionBenchmark.RunAsync(connectionString, mixedExpressConnection, Path.GetFullPath(args[1]), embeddingModel, workers);

    return;
}

const string question = "How are document access permissions enforced for a requesting user?";
const int topK = 5;
const double scoreTolerance = 0.00001;

// Generate once; both search implementations use these exact values.
string queryCachePath = @"D:\RagBenchmarkCache\queries\authorization-v1.json";

CachedBenchmarkQuery savedQuery;

if (File.Exists(queryCachePath))
{
    string json = await File.ReadAllTextAsync(queryCachePath);

    savedQuery = JsonSerializer.Deserialize<CachedBenchmarkQuery>(json) ?? throw new InvalidOperationException("The saved question embedding is invalid.");

    Console.WriteLine("Reusing saved question embedding.");
}
else
{
    var embedding = await generator.GenerateAsync(question);

    savedQuery = new CachedBenchmarkQuery(SchemaVersion: 1, EmbeddingModel: embeddingModel, Question: question, Embedding: embedding.Vector.ToArray());

    if (savedQuery.Embedding.Length != 1536 || savedQuery.Embedding.Any(value => !float.IsFinite(value)) || !savedQuery.Embedding.Any(value => value != 0))
    {
        throw new InvalidOperationException("The generated question embedding is invalid.");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(queryCachePath)!);

    string temporaryPath = queryCachePath + $".{Guid.NewGuid():N}.tmp";

    try
    {
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(savedQuery));

        File.Move(temporaryPath, queryCachePath);
    }
    finally
    {
        if (File.Exists(temporaryPath))
            File.Delete(temporaryPath);
    }

    Console.WriteLine("Generated and saved question embedding.");
}

if (savedQuery.SchemaVersion != 1 ||
    savedQuery.EmbeddingModel != embeddingModel ||
    savedQuery.Question != question ||
    savedQuery.Embedding is null ||
    savedQuery.Embedding.Length != 1536 ||
    savedQuery.Embedding.Any(value => !float.IsFinite(value)) ||
    !savedQuery.Embedding.Any(value => value != 0))
{
    throw new InvalidOperationException("Saved question metadata or vector does not match this benchmark. Use a different cache filename for a different question or model.");
}

float[] queryEmbedding = savedQuery.Embedding;


if (queryEmbedding.Length != 1536)
{
    throw new InvalidOperationException("Expected a 1536-dimensional question embedding.");
}

// Native SQL Server search: lower distance is better.
var queryVector = new SqlVector<float>(queryEmbedding);

var nativeQuery = db.DocumentChunks
    .AsNoTracking()
    .Select(chunk => new
    {
        ChunkId = chunk.Id,
        Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)
    })
    .OrderBy(match => match.Distance)
    .ThenBy(match => match.ChunkId)
    .Take(topK);

var nativeMatches = await nativeQuery.ToListAsync();

// Cached C# search: load once, then search vectors already in memory.
var cachedVectors = await LegacyVectorLoader.LoadAsync(db);

var cachedMatches = InMemoryVectorSearch.Search(queryEmbedding, cachedVectors, topK);

if (nativeMatches.Count != topK || cachedMatches.Count != topK)
{
    throw new InvalidOperationException("This comparison requires at least five chunks in both tables.");
}

Console.WriteLine($"QUESTION: {question}");
Console.WriteLine();
Console.WriteLine("Rank | SQL ID | C# ID | SQL similarity | C# similarity");

for (int index = 0; index < topK; index++)
{
    var native = nativeMatches[index];
    var cached = cachedMatches[index];

    // Cosine distance = 1 - cosine similarity.
    double nativeSimilarity = 1.0 - native.Distance;

    Console.WriteLine($"{index + 1,4} | {native.ChunkId,6} | {cached.ChunkId,5} | " + $"{nativeSimilarity:F8} | {cached.Similarity:F8}");

    if (native.ChunkId != cached.ChunkId || !double.IsFinite(nativeSimilarity) || !double.IsFinite(cached.Similarity) || Math.Abs(nativeSimilarity - cached.Similarity) > scoreTolerance)
    {
        throw new InvalidOperationException($"Search results differ at rank {index + 1}.");
    }
}

Console.WriteLine();
Console.WriteLine("PASS: rankings and similarity scores agree.");

Console.WriteLine();
Console.WriteLine($"Cached vectors: {cachedVectors.Count}");
Console.WriteLine($"Top K: {topK}");
Console.WriteLine("Concurrent searches: 1");
Console.WriteLine("Ranking only; cache loading excluded.");


expressConnection = builder.Configuration.GetConnectionString("ExpressConnection") ?? throw new InvalidOperationException("ExpressConnection is missing.");

var expressOptions = new DbContextOptionsBuilder<AInDotNetRAG_ExpressContext>().UseSqlServer(expressConnection).Options;

await using var expressDb = new AInDotNetRAG_ExpressContext(expressOptions);

var expressQuery = expressDb.DocumentChunks
    .AsNoTracking()
    .Select(chunk => new
    {
        ChunkId = chunk.Id,
        Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)
    })
    .OrderBy(match => match.Distance)
    .ThenBy(match => match.ChunkId)
    .Take(topK);

var expressMatches = await expressQuery.ToListAsync();

if (expressMatches.Count != nativeMatches.Count)
{
    throw new InvalidOperationException("Express and Developer returned different result counts.");
}

Console.WriteLine();
Console.WriteLine("Rank | Developer ID | Express ID | Express similarity");

for (int index = 0; index < nativeMatches.Count; index++)
{
    var developer = nativeMatches[index];
    var express = expressMatches[index];

    Console.WriteLine($"{index + 1,4} | {developer.ChunkId,12} | " + $"{express.ChunkId,10} | {1.0 - express.Distance:F8}");

    if (developer.ChunkId != express.ChunkId || !double.IsFinite(express.Distance) || Math.Abs(developer.Distance - express.Distance) > scoreTolerance)
    {
        throw new InvalidOperationException($"Express differs from Developer at rank {index + 1}.");
    }
}

Console.WriteLine("PASS: Express and Developer rankings and scores agree.");



// Preparation is outside search timing.
var preparedCache = new PreparedVectorCache(cachedVectors);

var preparedMatches = PreparedVectorSearch.Search(queryEmbedding, preparedCache, topK);

if (preparedMatches.Count != cachedMatches.Count)
{
    throw new InvalidOperationException("Prepared search returned a different result count.");
}

for (int index = 0; index < cachedMatches.Count; index++)
{
    if (preparedMatches[index].ChunkId != cachedMatches[index].ChunkId || Math.Abs(preparedMatches[index].Similarity - cachedMatches[index].Similarity) > scoreTolerance)
    {
        throw new InvalidOperationException($"Prepared search differs at rank {index + 1}.");
    }
}

Console.WriteLine("PASS: prepared C# search agrees with the original.");


#if DEBUG
Console.WriteLine("\r\nBuild configuration: DEBUG");
#else
Console.WriteLine("Build configuration: RELEASE");
#endif

Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

Console.WriteLine($"Process architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");

Console.WriteLine($"Debugger attached: {System.Diagnostics.Debugger.IsAttached}");


var developerWorkerOptions = new DbContextOptionsBuilder<AInDotNetRAG_BenchmarkContext>().UseSqlServer(connectionString).Options;

// Developer completes before the Express benchmark begins.

await ConcurrentRetrievalTimer.MeasureAsync("Express native SQL — 10,000 chunks, sixteen workers", () => new AInDotNetRAG_ExpressContext(expressOptions), async workerDb =>
{
    var matches = await workerDb.DocumentChunks
        .AsNoTracking()
        .Select(chunk => new
        {
            ChunkId = chunk.Id,
            Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)
        })
        .OrderBy(match => match.Distance)
        .ThenBy(match => match.ChunkId)
        .Take(topK)
        .ToListAsync();

    return matches.Count;
},
    expectedResults: topK);

await ConcurrentRetrievalTimer.MeasureAsync("Developer native SQL — 10,000 chunks, sixteen workers", () => new AInDotNetRAG_BenchmarkContext(developerWorkerOptions),
    async workerDb =>
    {
        var matches = await workerDb.DocumentChunks
            .AsNoTracking()
            .Select(chunk => new
            {
                ChunkId = chunk.Id,
                Distance = EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)
            })
            .OrderBy(match => match.Distance)
            .ThenBy(match => match.ChunkId)
            .Take(topK)
            .ToListAsync();

        return matches.Count;
    },
    expectedResults: topK);




//await RetrievalTimer.MeasureAsync("Developer native SQL ranking", async () => (await nativeQuery.ToListAsync()).Count, topK);

//await RetrievalTimer.MeasureAsync("Express native SQL ranking", async () => (await expressQuery.ToListAsync()).Count, topK);

//await RetrievalTimer.MeasureAsync("Cached C# ranking", () => Task.FromResult(InMemoryVectorSearch.Search(queryEmbedding, cachedVectors, topK).Count), topK);

//// Measure cosine calculations without allocating match objects
//// or selecting the top K.
//double similarityChecksum = 0;

//await RetrievalTimer.MeasureAsync("Cached C# cosine loop only — no ranking", () =>
//    {
//        double sum = 0;

//        foreach (var vector in cachedVectors)
//        {
//            sum += CosineSimilarity.Calculate(queryEmbedding, vector.Embedding);
//        }

//        // Keep the calculated result observable.
//        similarityChecksum = sum;

//        return Task.FromResult(cachedVectors.Count);
//    },
//    cachedVectors.Count);

//Console.WriteLine($"Similarity checksum: {similarityChecksum:F8}");


//await RetrievalTimer.MeasureAsync("Prepared cached C# ranking", () => Task.FromResult(PreparedVectorSearch.Search(queryEmbedding, preparedCache, topK).Count), topK);

//await RetrievalTimer.MeasureAsync("JSON load + deserialize + C# ranking (SQL Server 2025)",
//    async () =>
//    {
//        var vectors = await LegacyVectorLoader.LoadAsync(db);

//        var matches = InMemoryVectorSearch.Search(queryEmbedding, vectors, topK);

//        return matches.Count;
//    },
//    topK);

