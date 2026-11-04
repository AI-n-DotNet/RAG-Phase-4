# SQL Server Vector Retrieval Benchmark Results

## Purpose

Evaluate whether SQL Server 2025 Express provides useful vector-retrieval performance for .NET applications, and compare it with SQL Server 2025 Enterprise Developer Edition.

These measurements cover retrieval—not complete RAG response time.

## Environment

| Item 					| Configuration 
|---					|---
| Computer 				| Laptop; client and both database instances share hardware 
| OS Name				| Microsoft Windows 11 Pro
| Version				| 10.0.26200 Build 26200
| CPU 					| Intel Core Ultra 9 275HX; 24 cores / 24 logical processors 
| Installed RAM 		| 64 GB |
| Hard Drive			| SSD NVMe PM9E1 SED Samsung
| Developer instance 	| SQL Server 2025 Enterprise Developer, 64-bit 
| Express instance 		| SQL Server 2025 Express, 64-bit 
| SQL instances 		| Both 2025 instances running; tested sequentially; on same hard drive |
| Application 			| .NET 10.0.14, x64, Release |
| Debugger 				| Detached |



### SQL Server configuration recorded October 29, 2026

Values below are running values unless otherwise noted.

| Setting 							| Enterprise Developer 	| Express 
|---								|---					|---
| Product version 					| 17.0.1145.4 			| 17.0.1145.4 
| Product level 					| RTM 					| RTM 
| Product update level 				| NULL—not reported 	| NULL—not reported 
| Database compatibility level 		| 170 					| 170 
| Cost threshold for parallelism 	| 5 					| 5 
| MAXDOP 							| 8 					| 0 
| Maximum server memory, MB 		| 2,147,483,647 		| 2,147,483,647 
| Minimum server memory, MB 		| 0 / 16 				| 0 / 16 

Express settings were recorded at 2:27 p.m. Eastern; Developer settings at 2:30 p.m. Eastern.

The maximum-memory setting does not represent allocated memory or override edition limits. Both instances share the laptop’s physical RAM.

MAXDOP 0 does not mean parallelism is disabled. However, the captured Express query plan explicitly reported serial execution because of its edition. Developer’s captured plan used eight parallel workers, consistent with its MAXDOP setting of 8.

These settings were recorded after the benchmark runs. They document the observed configuration at collection time; historical consistency was not independently verified.



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
- This is a controlled synthetic workload. It is not a representative HR corpus or a retrieval-quality evaluation.
- The results below compare SQL Server 2025 Enterprise Developer and Express using matching workloads. The memory-cached C# implementation was tested on a subset of scenarios and is not included in this results table.

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

| Workers 	| Edition 				| Searches per run 	| Searches/sec 	| Median ms 	| p95 ms 			| p99 ms 
|---		|---					|---				|---			|---			|---				|---
| 4 		| Enterprise Developer 	| 4,000 			| 585.8–588.0 	| 4.957–5.082 	| 13.004–13.572 	| 16.178–19.542 
| 4 		| Express 				| 4,000 			| 203.2–210.0 	| 18.264–18.332 | 27.465–32.151 	| 30.630–37.030 
| 8 		| Enterprise Developer 	| 8,000 			| 655.6–664.6 	| 9.501–9.663 	| 24.542–25.151 	| 31.133–38.461 
| 8 		| Express 				| 8,000 			| 205.5–210.8 	| 30.881–39.911 | 60.652–67.759 	| 69.567–76.905 
| 16 		| Enterprise Developer 	| 16,000 			| 586.8–705.7 	| 20.802–21.995 | 55.149–101.330 	| 66.871–134.851 
| 16 		| Express 				| 16,000 			| 193.8–217.1 	| 51.326–53.575 | 152.652–158.149 	| 177.217–181.115 

All runs in this table reported zero failed searches.

## Execution-Plan Investigation

Separate actual-plan captures in SSMS showed:

| Captured query 						| Execution 				| Elapsed ms 	| CPU ms 
|---									|---						|---			|---
| Enterprise Developer 					| Eight parallel workers 	| 4 			| 20 
| Express 								| Serial 					| 13 			| 13 
| Enterprise Developer with `MAXDOP 1` 	| Serial 					| 15 			| 15 

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

## Configure User Secrets on the `AInDotNet.RagDemo.Benchmarks` project:

- `ConnectionStrings:BenchmarkConnection`: Developer benchmark database.
- `ConnectionStrings:ExpressConnection`: Express benchmark database.
- `OpenAI:EmbeddingModel`: the model used for the saved embeddings (`text-embedding-3-small` in these measurements).
- `OpenAI:ApiKey`: required by the current startup code, even when using saved embeddings.

The `verify-questions` and `benchmark-mixed` commands reuse saved question embeddings and do not make embedding-generation requests.

## Reproduction

These commands assume both benchmark databases are already populated with the same 10,000-chunk dataset and the ten saved question-embedding files are available. The examples use `.\RagBenchmarkCache\queries`, relative to the repository root; substitute your existing cache directory if it is elsewhere.

From the repository root:

```powershell
dotnet run --project .\AInDotNet.RagDemo.Benchmarks -c Release --no-launch-profile -- verify-questions ".\RagBenchmarkCache\queries"
```

Then:

```powershell
dotnet run --project .\AInDotNet.RagDemo.Benchmarks -c Release --no-launch-profile -- benchmark-mixed ".\RagBenchmarkCache\queries"
```

The `benchmark-mixed` command accepts an optional worker count of 4, 8, or 16. Omitting it defaults to 16. For example, append `4` after the query-cache directory to run four workers per edition.

Express runs first, followed by Developer. Both editions use the selected worker count, the same ten saved questions, top-five selection, 100 warm-up searches per worker, and 1,000 measured searches per worker.

The results table includes runs with reversed edition order. Execution order remains hard-coded in `MixedQuestionBenchmark.cs`; the command does not automatically reproduce the complete matrix. Record the selected worker count, edition order, and Git commit for each run.
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