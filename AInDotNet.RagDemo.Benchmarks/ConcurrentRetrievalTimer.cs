using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace AInDotNet.RagDemo.Benchmarks;

public static class ConcurrentRetrievalTimer
{
    public static async Task MeasureAsync<TContext>(string name, Func<TContext> createContext, Func<TContext, Task<int>> search, int expectedResults, int workers = 16, int searchesPerWorker = 5000) where TContext : DbContext
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workers);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(searchesPerWorker);

        var contexts = new List<TContext>(workers);

        try
        {
            for (int worker = 0; worker < workers; worker++)
                contexts.Add(createContext());

            // Warm every worker's context before starting measurement.
            foreach (var context in contexts)
            {
                for (int index = 0; index < 100; index++)
                {
                    if (await search(context) != expectedResults)
                    {
                        throw new InvalidOperationException("Unexpected result count during warm-up.");
                    }
                }
            }

            var durations = new ConcurrentBag<double>();
            var failures = new ConcurrentQueue<string>();

            var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var tasks = contexts.Select(async context =>
            {
                await startGate.Task;

                // Each worker uses only its own context, sequentially.
                for (int index = 0; index < searchesPerWorker; index++)
                {
                    long started = Stopwatch.GetTimestamp();

                    try
                    {
                        int count = await search(context);

                        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

                        if (count != expectedResults)
                        {
                            throw new InvalidOperationException($"Expected {expectedResults} results; got {count}.");
                        }

                        durations.Add(elapsed);
                    }
                    catch (Exception exception)
                    {
                        failures.Enqueue($"{exception.GetType().Name}: {exception.Message}");
                    }
                }
            }).ToArray();

            long runStarted = Stopwatch.GetTimestamp();

            startGate.SetResult(true);
            await Task.WhenAll(tasks);

            double seconds = Stopwatch.GetElapsedTime(runStarted).TotalSeconds;

            double[] sorted = durations.OrderBy(value => value).ToArray();

            Console.WriteLine();
            Console.WriteLine(name);
            Console.WriteLine($"Concurrent workers: {workers}");
            Console.WriteLine($"Attempts: {workers * searchesPerWorker}");
            Console.WriteLine($"Successful searches: {sorted.Length}");
            Console.WriteLine($"Failures: {failures.Count}");
            Console.WriteLine($"Measured elapsed time: {seconds:F3} s");
            Console.WriteLine($"Successful searches/second: {sorted.Length / seconds:F1}");

            if (sorted.Length > 0)
            {
                double median = sorted.Length % 2 == 0 ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2 : sorted[sorted.Length / 2];

                double Percentile(double fraction) => sorted[(int)Math.Ceiling(sorted.Length * fraction) - 1];

                Console.WriteLine("Latency percentiles below include successful searches only.");
                Console.WriteLine($"Median: {median:F3} ms");
                Console.WriteLine($"95th percentile: {Percentile(0.95):F3} ms");
                Console.WriteLine($"99th percentile: {Percentile(0.99):F3} ms");
            }

            foreach (string failure in failures.Take(3))
                Console.WriteLine($"Failure example: {failure}");
        }
        finally
        {
            foreach (var context in contexts)
                await context.DisposeAsync();
        }
    }
}