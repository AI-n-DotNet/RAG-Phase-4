using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Generation;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public static class ClaimVerificationReferenceCases
{
    private static readonly IReadOnlyList<RagSource> RequirementsEvidence =
    [
        new(
            "S1",
            69,
            1,
            "Enterprise RAG Architecture",
            "Enterprise RAG systems must enforce authorization, data classification, source attribution, and auditing. A document should not become accessible to a user merely because it is semantically relevant.",
            0.37)
    ];

    private static readonly IReadOnlyList<RagSource> ImplementationEvidence =
    [
        new(
            "S1",
            100,
            1,
            "RAG Demonstration Security Status",
            "The current RAG demonstration application does not implement document-level authorization. Retrieved documents are not filtered according to the requesting user's permissions.",
            0.25)
    ];

    private static readonly IReadOnlyList<RagSource> OAuthEvidence =
    [
        new(
            "S1",
            49,
            2,
            "OAuth Authorization",
            "OAuth 2.0 provides a framework for delegated authorization to protected resources.",
            0.42)
    ];

    public static IReadOnlyList<ClaimVerificationReferenceCase> All =>
    [
        new()
        {
            Id = "CLAIM-001",
            Claim = "Enterprise RAG systems must enforce authorization.",
            Sources = RequirementsEvidence,
            ExpectedOutcome = ClaimSupportOutcome.Supported
        },

        new()
        {
            Id = "CLAIM-002",
            Claim = "The RAG demonstration application currently enforces document-level authorization.",
            Sources = RequirementsEvidence,
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },

        new()
        {
            Id = "CLAIM-003",
            Claim = "The RAG demonstration application currently enforces document-level authorization.",
            Sources = ImplementationEvidence,
            ExpectedOutcome = ClaimSupportOutcome.Contradicted
        },

        new()
        {
            Id = "CLAIM-004",
            Claim = "OAuth 2.0 provides a framework for delegated authorization.",
            Sources = OAuthEvidence,
            ExpectedOutcome = ClaimSupportOutcome.Supported
        },

        new()
        {
            Id = "CLAIM-005",
            Claim = "The demonstration application uses JWT claims to filter retrieved documents.",
            Sources = OAuthEvidence,
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },

        new()
        {
            Id = "CLAIM-006",
            Claim = "The supplied passages do not identify which authorization controls are implemented in the RAG demonstration application.",
            Sources = RequirementsEvidence,
            ExpectedOutcome = ClaimSupportOutcome.Supported
        },
        new()
        {
            Id = "CLAIM-007",
            Claim = "Because OAuth 2.0 supports delegated authorization, the RAG demonstration application must use JWT claims to filter retrieved documents.",
            Sources = OAuthEvidence,
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },

        new()
        {
            Id = "CLAIM-008",
            Claim = "Because enterprise RAG systems must enforce authorization, the RAG demonstration application necessarily implements document-level authorization.",
            Sources = RequirementsEvidence,
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },
        new()
        {
            Id = "CLAIM-009",
            Claim = "Enterprise RAG systems must enforce authorization and the demonstration application currently implements document-level authorization.",
            Sources = RequirementsEvidence,
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },

        new()
        {
            Id = "CLAIM-010",
            Claim = "OAuth 2.0 provides delegated authorization and the demonstration application uses JWT claims to filter retrieved documents.",
            Sources = OAuthEvidence,
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },
        new()
        {
            Id = "CLAIM-011",
            Claim = "The demonstration application uses SQL Server for vector storage and OpenAI for embeddings.",
            Sources =
            [
                new RagSource(
                    "S1",
                    101,
                    1,
                    "Vector Storage",
                    "The demonstration application stores document embeddings in SQL Server using the VECTOR data type.",
                    0.1),

                new RagSource(
                    "S2",
                    102,
                    2,
                    "Embedding Generation",
                    "The demonstration application generates embeddings using OpenAI's text-embedding-3-small model.",
                    0.2)
            ],
            ExpectedOutcome = ClaimSupportOutcome.Supported
        },
        new()
        {
            Id = "CLAIM-012",
            Claim = "The demonstration application currently implements document-level authorization.",
            Sources =
            [
                new RagSource(
                    "S1",
                    103,
                    1,
                    "Authorization Architecture",
                    "The demonstration application currently implements document-level authorization.",
                    0.1),

                new RagSource(
                    "S2",
                    104,
                    2,
                    "Implementation Status",
                    "The demonstration application does not currently implement document-level authorization.",
                    0.2)
            ],
            ExpectedOutcome = ClaimSupportOutcome.InsufficientEvidence
        },
    ];
}