using AInDotNet.RagDemo.BenchmarkData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = Host.CreateApplicationBuilder(args);

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


string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";



using var host = builder.Build();

// -----------------------   END OF SETUP -----------------------


using var scope = host.Services.CreateScope();
