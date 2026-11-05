using AInDotNet.RagDemo.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public static class FaithfulnessReferenceCases
{
    public static FaithfulnessEvaluationResult Authorization => new()
    {
        EvaluationCaseId = "RAG-SECURITY-002",

        Claims =
        [
            new()
            {
                Claim = "Enterprise RAG systems must enforce authorization.",
                Outcome = ClaimSupportOutcome.Supported,
                SupportingSourceLabels = ["S1"],
                Explanation = "Chunk 69 explicitly states this requirement."
            },

            new()
            {
                Claim = "OAuth 2.0 provides a framework for delegated authorization.",
                Outcome = ClaimSupportOutcome.Supported,
                SupportingSourceLabels = ["S5"],
                Explanation =  "Chunk 49 explicitly describes OAuth 2.0."
            },

            new()
            {
                Claim = "Our RAG demonstration application currently enforces document-level authorization.",
                Outcome = ClaimSupportOutcome.InsufficientEvidence,
                Explanation =  "The retrieved passages do not establish that this control is implemented."
            }
        ]
    };
}