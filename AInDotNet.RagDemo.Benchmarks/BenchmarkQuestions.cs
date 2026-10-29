using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

public sealed record BenchmarkQuestion(string CacheName, string Text);

public static class BenchmarkQuestions
{
    public static IReadOnlyList<BenchmarkQuestion> All { get; } = Array.AsReadOnly(new BenchmarkQuestion[]
        {
            new("authorization-v1", "How are document access permissions enforced for a requesting user?"),

            new("backups-v1", "What should database backups include, and how are restores verified?"),

            new("ingestion-v1", "How does document ingestion prepare text for retrieval?"),

            new("inventory-v1", "How are stock levels and replenishment requests managed?"),

            new("invoices-v1", "How are invoice payment deadlines and approvals recorded?"),

            new("incidents-v1", "What information is recorded during incident response?"),

            new("onboarding-v1", "What tasks are involved in employee onboarding?"),

            new("deployment-v1", "How are software releases staged and rolled back?"),

            new("maintenance-v1", "How are equipment inspections and replacements scheduled?"),

            new("support-v1", "How are customer support requests tracked through resolution?")
        });
}