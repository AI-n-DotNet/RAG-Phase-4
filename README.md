# RAG with SQL Server, EF Core, and .NET

A runnable demonstration of Retrieval-Augmented Generation using familiar .NET development practices: SQL Server for vector storage and retrieval, EF Core for data access, and OpenAI for embeddings and answer generation.

Designed for developers who already understand C#, .NET, SQL Server, and basic RAG concepts.

**A specialized vector database is not a prerequisite for RAG.** This repository demonstrates implementation and evaluation. It does not yet establish performance at scale.

## Prerequisites

- **SQL Server 2025 (17.x) is required.** SQL Server 2022 and earlier lack the native vector functionality used here. Updating SSMS does not upgrade the database engine.
- **SQL Server 2025 Developer Edition** was used to test this demonstration. It is free for development and testing and is not licensed for production use.
- **SQL Server 2025 Express Edition** is also free and supports the native vector functionality used here, so we expect this application to work with it. **We have not tested this application on Express.** Express permits production use within its edition limits.
- Express has lower CPU and memory limits than Developer Edition. These limits may reduce performance as data volume and concurrent usage increase. **We have not measured the performance difference; small workloads may show little difference.**
- .NET 10 SDK and a compatible Visual Studio installation.  In October 2026 - we use Visual Studio 2026 and .NET 10
- SQL Server Management Studio (SSMS).  We used version 22.10.2
- An OpenAI API key with access to the configured models. Running ingestion, questions, and live integration tests incurs API usage charges.

## Solution structure

| Project | Responsibility |
|---|---|
| `AInDotNet.RagDemo` | Console application and dependency injection |
| `AInDotNet.RagDemo.Application` | Text loading, chunking, ingestion, retrieval, and grounded answers |
| `AInDotNet.RagDemo.Data` | EF Core database models reverse engineered with EF Core Power Tools |
| `AInDotNet.RagDemo.Tests` | Automated tests and the current evaluation framework |

The database remains the source of truth for the reverse-engineered data model.

## 1. Open the solution

Clone this repository and open:

```text
AInDotNet.RagDemo/AInDotNet.RagDemo.slnx
```

Set `AInDotNet.RagDemo` as the startup project.

## 2. Create the database

In SSMS, connect to your **SQL Server 2025** instance and execute:

```text
Database/CreateRagSchema.sql
```

The script creates `AInDotNetRAG`, its tables, indexes, foreign key, and creation-date default. It uses the SQL Server instance’s default data and log directories.

**Run it once against an instance where `AInDotNetRAG` does not already exist.** The script does not populate documents or embeddings.

The `DocumentChunks.Embedding` column stores 1,536-dimensional vectors.

## 3. Configure credentials

In Visual Studio, right-click the console project and select **Manage User Secrets**. Configure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_INSTANCE;Database=AInDotNetRAG;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY",
    "EmbeddingModel": "text-embedding-3-small",
    "ChatModel": "gpt-4.1-mini"
  }
}
```

Replace the server instance and API key. The connection example uses Windows authentication and trusts the server certificate for local development; configure certificate validation appropriately for other environments.

Keep credentials in User Secrets, outside the repository.

The console and test projects currently share a User Secrets ID, so these settings are available to both.

The application requires **1,536-dimensional embeddings**. Changing embedding models requires compatible dimensions and re-embedding existing content; matching dimensions alone does not make different models interchangeable.

## 4. Ingest a text file

Create a UTF-8 text file, for example `D:\RagSamples\TestInputFile01.txt`:

```text
The AInDotNet demonstration stores document embeddings in SQL Server.
```

In the console project’s debug launch settings, set the command-line arguments to:

```text
ingest "D:\RagSamples\TestInputFile01.txt"
```

Run with **Ctrl+F5**.

Alternatively, from the repository root:

```powershell
dotnet run --project .\AInDotNet.RagDemo -- ingest "D:\RagSamples\TestInputFile01.txt"
```

The application reads the file, divides its text into overlapping chunks, generates embeddings, and saves the document and chunks through EF Core.

Example output:

```text
Document: TestInputFile01
Document ID: 65
Chunks: 1
Document ingested.
```

IDs vary by database.

Run the same command again to verify duplicate detection:

```text
Document already exists; ingestion skipped.
```

Documents are identified by their absolute file URI. **Editing an already-ingested file does not update its stored content.** Moving or renaming the file changes its source identity.

## 5. Ask a question

Replace the debug command-line arguments with:

```text
ask "Where does the AInDotNet demonstration store document embeddings?"
```

Run with **Ctrl+F5**, or use:

```powershell
dotnet run --project .\AInDotNet.RagDemo -- ask "Where does the AInDotNet demonstration store document embeddings?"
```

Example output:

```text
QUESTION: Where does the AInDotNet demonstration store document embeddings?

ANSWER:
The AInDotNet demonstration stores document embeddings in SQL Server [S1].

CITED SOURCES:
[S1] TestInputFile01 (Document 65, Chunk 71, Distance 0.112441)
```

Wording, source labels, IDs, and distances can vary.

Retrieval generates a question embedding and uses EF Core to execute cosine-distance calculations, ordering, and top-five selection inside SQL Server.

Citation cleanup ensures returned citation labels correspond to retrieved sources. It does not itself verify factual support; the evaluation framework performs separate support checks.

The `ingest` and `ask` commands bypass demonstration seeding. Running without arguments executes the existing seed-and-question demonstration.

## 6. Run the tests

Open **Test → Test Explorer** in Visual Studio.

- Run an individual test while making a focused change.
- Run the full suite at a checkpoint.

**Recorded development checkpoint: 141 tests passed.** This is a reported run, not a guarantee that every environment or subsequent live-model run will produce the same result.

Some tests use fakes; others call OpenAI, and some also require SQL Server. Tests are configured for method-level parallel execution, so combined test durations can exceed elapsed suite time.

### Fresh-database verification

The create → configure → ingest → ask workflow was manually verified
on SQL Server 2025 Developer Edition using an initially empty database.
The application ingested a text file and answered a question with a
citation to that document.

SQL Server 2025 Express compatibility and performance remain untested.


### Fresh-database considerations

The schema script creates empty tables. The live evaluation tests were developed against an existing demonstration corpus, and some reference specific chunk IDs, including chunk 69.

**The schema plus your own sample document is sufficient for the ingest/ask walkthrough, but does not reproduce the complete integration-test fixture.** Running the console without arguments seeds demonstration content; it does not guarantee the same IDs or reproduce every previously ingested document.

## What is evaluated?

The test project includes checks for:

- Retrieval precision, recall, and reciprocal rank.
- Answer faithfulness against retrieved evidence.
- Claim extraction and preservation of source attribution.
- Citation support and coverage.
- Requirements versus verified implementation.
- Missing evidence versus explicit contradiction.

The full evaluator is exercised through tests. The `ask` command generates an answer and displays citations; it does not run the full evaluation pipeline.

## Current scope and limitations

- Plain-text (`.txt`) ingestion only.
- Existing source URIs are skipped; document updates and deletion are not implemented.
- Retrieval uses exact cosine-distance search, without an approximate vector index.
- No hybrid retrieval, reranking, or retrieval-distance cutoff.
- No user-specific document authorization filtering.
- Evaluation implementation currently resides in the test project.
- Live model responses can vary.
- No published workload benchmark establishes latency, concurrency, or scaling limits.

Database-side retrieval avoids transferring every embedding into application memory, but exact search can still evaluate every qualifying vector inside SQL Server.

Use this repository to understand and reproduce the approach. Measure retrieval quality, latency, throughput, and resource consumption against your own workload before drawing production performance conclusions.