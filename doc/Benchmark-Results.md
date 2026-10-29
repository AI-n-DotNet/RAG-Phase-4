# SQL Server Vector Retrieval Benchmark Results

## Purpose

Evaluate whether SQL Server 2025 Express provides useful vector-retrieval performance for .NET applications, and compare it with SQL Server 2025 Enterprise Developer Edition.

These measurements cover retrieval—not complete RAG response time.

## Environment

| Item | Configuration |
|---|---|
| Computer | Laptop; client and both database instances share hardware |
| CPU | Intel Core Ultra 9 275HX; 24 cores / 24 logical processors |
| Installed RAM | 64 GB |
| Developer instance | SQL Server 2025 Enterprise Developer, 64-bit |
| Developer build | 17.0.1145.4, RTM |
| Express instance | SQL Server 2025 Express, 64-bit |
| Express build | Not yet recorded |
| Database compatibility | 170 |
| Application | .NET 10.0.14, x64, Release |
| Debugger | Detached |
| SQL Server 2022 instance | Stopped for the concurrency experiments |
| Other SQL instances | Both 2025 instances running; tested sequentially |

Before publication, also record Windows version, storage type, Express build, SQL memory settings, MAXDOP, cost threshold for parallelism, and the Git commit used for each result set.

## Dataset

- 10,000 distinct synthetic enterprise passages.
- Corpus identifier: `enterprise-synthetic-v1`.
- The expanded corpus preserves the original 1,000 passages.
- Additional passages use related templates with fictional deployment details.
- Model: `text-embedding-3-small`.
- Dimensions: 1,536; stored as 32-bit floating-point values.
- Identical saved embeddings, chunk IDs, and text imported into both instances.
- Ten fixed questions covering authorization, backups, ingestion, inventory, invoices, incidents, onboarding, deployment, maintenance, and support.
- Question embeddings generated beforehand and reused.
- Top-five IDs and scores agreed across editions for all ten questions.

This is a controlled synthetic workload. It is not a representative HR corpus or a retrieval-quality evaluation.

## What Is Timed

Each search includes:

1. EF Core query execution.
2. Database round trip.
3. Exact cosine-distance calculation over stored vectors.
4. Top-five selection.
5. Materialization of five chunk IDs and distance scores.

It excludes:

- Document ingestion and embedding generation.
- Question embedding generation.
- Fetching passage text.
- Authorization filters, hybrid retrieval, and reranking.
- Language-model answer generation.

Queries use `AsNoTracking()`, order by distance and then chunk ID, and select five results. No approximate vector index is used.

## Measurement Method

- Each concurrent worker owns a separate EF Core context.
- Each worker issues searches sequentially, without pauses.
- Workers rotate deterministically through the ten saved questions.
- Each worker performs 100 warm-up searches before measurement.
- Measured searches per worker: 1,000.
- Developer and Express run sequentially.
- Each concurrency level was run twice, reversing edition order.
- Per-query console/database logging is disabled.
- Latency percentiles include successful searches only.
- Throughput equals successful searches divided by measured wall-clock time.
- A result-count mismatch or exception is recorded as a failure.

Workers represent outstanding retrieval requests, not a count of human users.

## Mixed-Question Results: 10,000 Vectors

Ranges below are the minimum and maximum reported statistics from two runs, not confidence intervals or pooled percentiles.

| Workers | Edition | Searches per run | Searches/sec | Median ms | p95 ms | p99 ms |
|---:|---|---:|---:|---:|---:|---:|
| 4 | Enterprise Developer | 4,000 | 585.8–588.0 | 4.957–5.082 | 13.004–13.572 | 16.178–19.542 |
| 4 | Express | 4,000 | 203.2–210.0 | 18.264–18.332 | 27.465–32.151 | 30.630–37.030 |
| 8 | Enterprise Developer | 8,000 | 655.6–664.6 | 9.501–9.663 | 24.542–25.151 | 31.133–38.461 |
| 8 | Express | 8,000 | 205.5–210.8 | 30.881–39.911 | 60.652–67.759 | 69.567–76.905 |
| 16 | Enterprise Developer | 16,000 | 586.8–705.7 | 20.802–21.995 | 55.149–101.330 | 66.871–134.851 |
| 16 | Express | 16,000 | 193.8–217.1 | 51.326–53.575 | 152.652–158.149 | 177.217–181.115 |

All runs in this table reported zero failed searches.

## Execution-Plan Investigation

Separate actual-plan captures in SSMS showed:

| Captured query | Execution | Elapsed ms | CPU ms |
|---|---|---:|---:|
| Enterprise Developer | Eight parallel workers | 4 | 20 |
| Express | Serial | 13 | 13 |
| Enterprise Developer with `MAXDOP 1` | Serial | 15 | 15 |

All three scanned 10,000 rows and reported zero physical reads.

The Express plan recorded:

`NoParallelPlansInDesktopOrExpressEdition`

The forced-serial Developer plan recorded:

`MaxDOPSetToOne`

These were individual SSMS executions, not repeated benchmark measurements. They support parallelism as an important explanation for the observed latency difference, but do not isolate every edition or configuration difference.

The normal benchmark does not use `MAXDOP 1`.

## Observations

- Express sustained approximately 200 searches per second in the mixed-question workload on this laptop.
- Increasing Express concurrency beyond four workers produced little additional throughput and substantially higher latency.
- Enterprise Developer sustained higher throughput, with more variability at sixteen workers.
- Neither edition reported failed searches at the tested loads.
- A throughput plateau and rising latency indicate saturation symptoms; they do not mean the database crashed.
- These results do not directly predict complete RAG latency or supported employee counts.

## Limitations

- Both database instances and the client share one laptop.
- Only one hardware configuration was tested.
- Two runs per concurrency level provide limited statistical evidence.
- The corpus is synthetic and contains closely related templates.
- Warmed execution was measured; cold-start behavior was not isolated.
- The worker model is closed-loop: workers wait for completion before issuing another request. It does not measure queues under an independently scheduled arrival rate.
- Database CPU queues were observed, but bottlenecks were not exhaustively isolated.
- Enterprise Developer results must not be presented as Standard Edition results.
- No SQL Server 2022 or competing database performance comparison has been completed.
- Early measurements used shorter warm-ups or an attached debugger and should not be mixed with the later benchmark results.

## Reproduction

From the repository root:

```powershell
dotnet run --project .\AInDotNet.RagDemo.Benchmarks -c Release --no-launch-profile -- verify-questions "D:\RagBenchmarkCache\queries"
```

Then:

```powershell
dotnet run --project .\AInDotNet.RagDemo.Benchmarks -c Release --no-launch-profile -- benchmark-mixed "D:\RagBenchmarkCache\queries"
```

The current runner uses hard-coded worker counts and call order. Record those settings and the commit hash for each run.

For each future run, save:

- Timestamp and Git commit.
- Dataset size and corpus/model identifiers.
- Worker count, warm-up count, and measured search count.
- Edition execution order.
- Full console output, including failures.
- Runtime/build configuration and relevant server settings.

Keep raw outputs alongside this summary, for example:

`docs/benchmarks/raw/10k-mixed-16workers-express-first.txt`

Do not include User Secrets, API keys, or credential-bearing connection strings.

## Conclusion

SQL Server 2025 Express is worth evaluating for small .NET RAG workloads. In this experiment, it performed exact vector retrieval using EF Core without a separate vector database product.

Enterprise Developer provided substantially higher throughput under load. Deployment decisions should follow application-specific latency, throughput, security, and operational requirements—not the best individual benchmark result.