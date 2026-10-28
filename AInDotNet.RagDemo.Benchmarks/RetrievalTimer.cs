using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace AInDotNet.RagDemo.Benchmarks;


public static class RetrievalTimer
{
    public static async Task MeasureAsync(string name, Func<Task<int>> search, int expectedResults)
    {
        const int warmups = 100;
        const int measurements = 200;

        for (int index = 0; index < warmups; index++)
        {
            int count = await search();

            if (count != expectedResults)
                throw new InvalidOperationException($"{name}: unexpected result count.");
        }

        var durations = new double[measurements];

        for (int index = 0; index < measurements; index++)
        {
            long started = Stopwatch.GetTimestamp();

            int count = await search();

            durations[index] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (count != expectedResults)
                throw new InvalidOperationException($"{name}: unexpected result count.");
        }

        Array.Sort(durations);

        double median = (durations[measurements / 2 - 1] + durations[measurements / 2]) / 2;

        int p95Index = (int)Math.Ceiling(measurements * 0.95) - 1;

        Console.WriteLine();
        Console.WriteLine(name);
        Console.WriteLine($"Measured searches: {measurements}");
        Console.WriteLine($"Median: {median:F3} ms");
        Console.WriteLine($"95th percentile: {durations[p95Index]:F3} ms");
        Console.WriteLine($"Minimum: {durations[0]:F3} ms");
        Console.WriteLine($"Maximum: {durations[^1]:F3} ms");
    }
}