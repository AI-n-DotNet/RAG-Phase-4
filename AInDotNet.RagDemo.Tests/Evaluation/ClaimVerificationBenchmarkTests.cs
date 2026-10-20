using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Integration")]
public sealed class ClaimVerificationBenchmarkTests
{
    [TestMethod]
    public async Task VerifyAsync_ReferenceCases_MatchExpectedOutcomes()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddUserSecrets<ClaimVerificationBenchmarkTests>();

        string apiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        string chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";

        builder.Services.AddSingleton<IChatClient>(_ => new OpenAIClient(apiKey).GetChatClient(chatModel).AsIChatClient());

        builder.Services.AddTransient<IClaimEvidenceVerifier, LlmClaimEvidenceVerifier>();

        using var host = builder.Build();

        var verifier = host.Services.GetRequiredService<IClaimEvidenceVerifier>();

        int correct = 0;
        var failures = new List<string>();

        // Act
        foreach (var referenceCase in ClaimVerificationReferenceCases.All)
        {
            var result = await verifier.VerifyAsync(referenceCase.Claim, referenceCase.Sources);

            if (result.Outcome != ClaimSupportOutcome.Supported)
            {
                Assert.AreEqual(0, result.SupportingSourceLabels.Count, $"{referenceCase.Id}: Non-supported claims must not return supporting source labels.");
            }
            if (referenceCase.Id == "CLAIM-011")
            {
                CollectionAssert.AreEquivalent(new[] { "S1", "S2" }, result.SupportingSourceLabels.ToArray(), "CLAIM-011 must attribute support to both retrieved sources.");
            }

            bool matched = result.Outcome == referenceCase.ExpectedOutcome;

            if (matched)
            {
                correct++;
            }
            else
            {
                failures.Add($"{referenceCase.Id}: expected " + $"{referenceCase.ExpectedOutcome}, " + $"actual {result.Outcome}");
            }

            Console.WriteLine($"{referenceCase.Id}: " + $"Expected={referenceCase.ExpectedOutcome}, " + $"Actual={result.Outcome}, " + $"Match={matched}");

            Console.WriteLine($"Explanation: {result.Explanation}");
            Console.WriteLine($"SupportingSourceLabels: " + $"{string.Join(", ", result.SupportingSourceLabels)}");

            Console.WriteLine();
        }

        // Report
        int total = ClaimVerificationReferenceCases.All.Count;

        double accuracy = (double)correct / total;

        Console.WriteLine("CLAIM VERIFICATION BENCHMARK:");
        Console.WriteLine($"Cases: {total}");
        Console.WriteLine($"Correct: {correct}");
        Console.WriteLine($"Accuracy: {accuracy:P1}");

        Assert.AreEqual(total, correct, $"Claim verification benchmark failed: " + $"{correct} of {total} cases matched expected outcomes.");

        Console.WriteLine();
        Console.WriteLine("CLASSIFICATION BREAKDOWN:");

        foreach (var expectedOutcome in Enum.GetValues<ClaimSupportOutcome>())
        {
            var cases = ClaimVerificationReferenceCases.All.Where(c => c.ExpectedOutcome == expectedOutcome).ToList();

            Console.WriteLine($"{expectedOutcome}: {cases.Count} reference cases");
        }

        if (failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("DISAGREEMENTS:");

            foreach (string failure in failures)
            {
                Console.WriteLine(failure);
            }
        }

        // Assert
        Assert.AreEqual(total, correct + failures.Count, "Every reference case must produce a classification.");

        Assert.IsTrue(accuracy >= 0 && accuracy <= 1, "Classification accuracy must be between 0 and 1.");
    }
}