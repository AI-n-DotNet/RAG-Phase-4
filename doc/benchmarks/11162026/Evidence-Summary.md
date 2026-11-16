# Phase 4 content evidence — November 16, 2026

Repository: D:\AInDotNet\CoreApplications\RAG\SqlServerVector-Phase4
Commit: f2b23c3d75703a8228baa524c74c9d6e62a33884
Git working tree clean before and after test execution. No source changes or database population commands performed.

## Automated checks

- Deterministic: 213 passed, 0 failed, 0 skipped.
- LiveEvaluation: 52 passed, 0 failed, 0 skipped. One run; no reruns to seek a passing result.
- Saved-question equivalence: all ten questions returned matching top-five IDs and scores within the verifier tolerance (0.00001).
- Configured model: gpt-4.1-mini. Configured embedding model: text-embedding-3-small. Names read from existing User Secrets; credentials not copied.
- .NET SDK 10.0.401, Release build. Earlier reported live failures remain relevant; this run does not establish consistent model reliability.
- Separate DatabaseIntegration and Performance test categories were not run. The retrieval benchmarks below were run explicitly instead.

## Fresh retrieval benchmark series

Six invocations: 4, 8, and 16 workers; Express-first and Developer-first for each. Edition runs sequential, with no other test suites running concurrently. Both databases contained 10,000 chunks according to the benchmark precondition. Ten saved question vectors; top five; 100 warm-up searches per worker; 1,000 measured searches per worker. Every run reported zero failures. Total successful measured searches: 112,000.

| Workers | Developer searches/sec | Express searches/sec | Developer p95 ms | Express p95 ms |
|---|---|---|---|---|
| 4 | 487.0–540.0 | 164.1–166.8 | 14.989–16.460 | 29.688–41.231 |
| 8 | 485.2–556.1 | 170.3–170.9 | 40.099–42.446 | 95.356–124.417 |
| 16 | 507.7–512.2 | 156.0–180.5 | 92.519–94.349 | 156.753–196.376 |

Ranges are the minimum and maximum of two run statistics, not confidence intervals or pooled percentiles. Matched-run throughput ratios are approximately 2.8–3.3 times in favor of the configured Developer instance. Latency ratios differ; avoid the generic claim "twice as fast."

## Publication interpretation

Recommend SQL Server 2025 Express as a free starting point for prototyping and testing. Microsoft documents edition upgrades from Express to Standard and Enterprise. Validate the intended production edition and application workload before deployment.

The repository documents its Developer instance as Enterprise Developer. This new run used the existing Developer and Express connection configuration; current server edition/version/settings were not independently captured. An attempted read-only metadata capture stopped locally because Windows authentication was not confirmed; no SQL query from that metadata check was executed. Do not assume that all historical environment settings remained unchanged.

These results measure retrieval, excluding question embedding generation, passage-text fetching, and answer generation. They do not establish employee capacity, retrieval quality on a real corpus, cold-start behavior, Standard Edition performance, production performance, or an Express-to-production migration rehearsal.

The historical raw four-worker outputs still disagree with the historical summary table. This dated series does not retroactively reconcile them. Use it as a separate series with its own raw files. Complete raw files include start/end timestamps and the Git commit; results.csv contains all twelve edition measurements.

## Evidence files

- deterministic/deterministic.trx
- live/live.trx
- benchmarks/verify-questions.txt
- benchmarks/results.csv
- benchmarks/{4,8,16}-workers-{express-first,developer-first}.txt

## Microsoft references checked November 16, 2026

- https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- https://learn.microsoft.com/en-us/sql/database-engine/install-windows/supported-version-and-edition-upgrades-2025?view=sql-server-ver17
