# Testing workflows

## Categories

| Category | Purpose | Dependencies |
|---|---|---|
| Deterministic | Regression checks using fixed inputs, fakes and local temporary files | No SQL Server, API key or model calls |
| LiveEvaluation | Retrieval, generation, extraction and model-judge quality checks | OpenAI; some also require the prepared SQL Server corpus |
| DatabaseIntegration | Database connection, storage, ingestion and retrieval checks | SQL Server; some also use OpenAI embeddings |
| Performance | Explicit performance measurements | Prepared SQL Server data and OpenAI |
| ExternalDependency | All tests requiring SQL Server or OpenAI | As described above |

Categories can overlap. The existing Integration label remains available.
In Visual Studio Test Explorer, group by **Traits** and select a category.
**Run All still executes every test.** No assertions have been removed, relaxed,
ignored or converted into passes.

## Deterministic regressions

From the repository root, using the current VSTest configuration:

~~~powershell
dotnet test .\AInDotNet.RagDemo.Tests\AInDotNet.RagDemo.Tests.csproj -c Release --filter "TestCategory=Deterministic" --results-directory .\TestResults\deterministic --logger "trx;LogFileName=deterministic.trx"
~~~

A passing deterministic subset establishes neither live model quality nor
database readiness. These checks require no credentials or external services.

## Live quality evaluations

~~~powershell
dotnet test .\AInDotNet.RagDemo.Tests\AInDotNet.RagDemo.Tests.csproj -c Release --filter "TestCategory=LiveEvaluation" --results-directory .\TestResults\live --logger "trx;LogFileName=live.trx"
~~~

These tests retain their strict assertions. Failed extraction, incorrect judge
verdicts and unsuccessful bounded repairs remain failures. API calls incur usage
charges. Some tests require the demonstration corpus and expected chunk IDs.

Raw extraction tests intentionally run without repair. Repair tests assess the
one-attempt boundary separately. End-to-end tests can enable bounded repair and
still fail if the repaired extraction is invalid.

Report live results separately from deterministic results. Preserve the model,
code version, timestamp, counts, original extraction and available repair
diagnostics. Do not rerun until a pass and discard preceding failures. A bounded
sample measures observed variability; it does not guarantee future behavior.
Do not describe a filtered green regression run as an all-tests-passing result
while live evaluations remain red.

## Database checks excluding live quality and performance evaluations

~~~powershell
dotnet test .\AInDotNet.RagDemo.Tests\AInDotNet.RagDemo.Tests.csproj -c Release --filter "TestCategory=DatabaseIntegration&TestCategory!=LiveEvaluation&TestCategory!=Performance" --results-directory .\TestResults\database --logger "trx;LogFileName=database.trx"
~~~

This group includes database-only checks and ingestion checks that call OpenAI.
Some tests insert rows inside rollback transactions. Use the intended
development/test databases; these are separate from deterministic checks.

Select performance tests explicitly with the filter **TestCategory=Performance**.
The deterministic and live-quality commands above do not select performance tests.

## Interpreting results

Separate code regressions, infrastructure/configuration failures and model-quality
failures. A completed end-to-end test does not imply perfect answer or citation scores.

The last complete run before categorization reported **212 tests: 209 passed and
3 failed**. The failures were raw live extraction, an ambiguous-status live judge,
and an unsuccessful bounded repair. Categorization does not resolve these findings.
Build and establish fresh per-category counts; no test cases were added or removed.

The commands use the current project configuration: EnableMSTestRunner=false,
with no repository-level MTP runner selection. Changing runners can change CLI syntax.

References: [selected-test filtering](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests)
and [MSTest runner configuration](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-running-tests).
