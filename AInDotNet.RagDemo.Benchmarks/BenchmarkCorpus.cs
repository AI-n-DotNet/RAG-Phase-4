using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

public static class BenchmarkCorpus
{
    public static IReadOnlyList<string> Create()
    {
        string[] topics =
        [
            "Database backups include transaction logs and restore verification.",
            "Document ingestion extracts text and divides it into overlapping chunks.",
            "Authorization filters documents according to the requesting user's permissions.",
            "Inventory management tracks stock levels and replenishment requests.",
            "Invoice processing records payment deadlines and approval decisions.",
            "Incident response records alerts, investigation findings, and corrective actions.",
            "Employee onboarding assigns training and verifies account provisioning.",
            "Software deployment uses staged releases and documented rollback procedures.",
            "Equipment maintenance records inspections and replacement schedules.",
            "Customer support tracks requests, ownership, and resolution history."
        ];

        string[] regions =
        [
            "North", "South", "East", "West", "Central", "Coastal", "Mountain", "Lakes", "Valley", "Capital"
        ];

        var passages = new List<string>(1000);

        for (int topic = 0; topic < topics.Length; topic++)
        {
            for (int region = 0; region < regions.Length; region++)
            {
                for (int system = 1; system <= 10; system++)
                {
                    string identifier = $"T{topic + 1:D2}-R{region + 1:D2}-S{system:D2}";

                    passages.Add($"Synthetic benchmark record {identifier}. " +
                        $"This example concerns system {system:D2} " +
                        $"in the {regions[region]} region. " +
                        $"{topics[topic]} " +
                        $"The assigned team reviews operational records " +
                        $"every {system + 1} days and retains review " +
                        $"evidence for {30 + region * 10} days. " +
                        $"An unresolved issue is escalated to the " +
                        $"{regions[region]} operations coordinator. " +
                        $"The procedure reference is {identifier}. " +
                        $"These details are fictional benchmark data, " +
                        $"not documentation of an implemented application.");
                }
            }
        }

        return passages;
    }

    public static IReadOnlyList<string> CreateTenThousand()
    {
        var original = Create();
        var passages = new List<string>(10000);

        // Preserve existing passage content, ordering, and cache IDs.
        passages.AddRange(original);

        for (int deployment = 1; deployment <= 9; deployment++)
        {
            foreach (string passage in original)
            {
                passages.Add(passage + " " +
                    $"Additional fictional deployment group {deployment:D2}. " +
                    $"This group has a review budget of " +
                    $"{deployment * 250} staff hours per quarter and " +
                    $"an escalation deadline of {deployment + 2} hours.");
            }
        }

        return passages;
    }
}