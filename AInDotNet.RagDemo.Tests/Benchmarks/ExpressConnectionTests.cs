using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.ExpressData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AInDotNet.RagDemo.Tests.Benchmarks;

[TestClass]
[TestCategory("Integration")]
public sealed class ExpressConnectionTests
{
    [TestMethod]
    public async Task ExpressConnection_CanReadBenchmarkTables()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<ExpressConnectionTests>().Build();

        string connectionString = configuration.GetConnectionString("ExpressConnection") ?? throw new InvalidOperationException("ExpressConnection is missing.");

        var options = new DbContextOptionsBuilder<AInDotNetRAG_ExpressContext>().UseSqlServer(connectionString).Options;

        await using var db = new AInDotNetRAG_ExpressContext(options);

        Assert.AreEqual("AInDotNetRAG_Benchmark", db.Database.GetDbConnection().Database);

        string edition = await db.Database.SqlQueryRaw<string>(
                """
                SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128))
                    AS [Value]
                """)
            .SingleAsync();

        Assert.IsTrue(edition.Contains("Express", StringComparison.OrdinalIgnoreCase), $"Expected Express, but connected to: {edition}");

        int documents = await db.Documents.CountAsync();
        int nativeChunks = await db.DocumentChunks.CountAsync();
        int jsonChunks = await db.LegacyBenchmarkChunks.CountAsync();

        Console.WriteLine($"Edition: {edition}");
        Console.WriteLine($"Documents: {documents}");
        Console.WriteLine($"Native chunks: {nativeChunks}");
        Console.WriteLine($"JSON chunks: {jsonChunks}");
    }
}