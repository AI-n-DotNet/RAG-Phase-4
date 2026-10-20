using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Tests.Evaluation;


public sealed class LlmClaimClassifier : IClaimClassifier
{
    private readonly IChatClient _chatClient;

    public LlmClaimClassifier(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<ClaimType> ClassifyAsync(string claim, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claim);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                """
                Classify the meaning of a claim for enterprise RAG evaluation.

                Return CollectionEvidence when the claim makes an
                assertion about the contents, coverage, completeness,
                or absence of information in the retrieved evidence
                collection.

                Return Factual when the claim asserts facts about
                applications, organizations, technologies, controls,
                or other subjects.

                Distinguish statements about missing documentation
                from statements about missing implementation.

                Classify by meaning, not keyword matching.

                CLASSIFICATION GUIDANCE:

                Factual:
                - Claims asserting architectural requirements, technical behavior, capabilities, implementations, or other substantive facts.
                - Claims attributing a substantive factual assertion to a source, such as "The retrieved sources state that enterprise RAG requires authorization."
                - A claim does not become CollectionEvidence merely because it begins with "The retrieved sources state" or similar wording.

                CollectionEvidence:
                - Claims specifically describing what information the retrieved collection contains, lacks, does not establish, or cannot confirm.
                - Claims about the limits of available evidence, especially whether implementation status can be determined.

                Examples:

                "Enterprise RAG systems must enforce authorization."
                Factual

                "The retrieved sources state that enterprise RAG requires authorization."
                Factual

                "The retrieved passages do not establish which authorization controls are implemented."
                CollectionEvidence

                "The implementation status cannot be determined from the retrieved evidence."
                CollectionEvidence

                Respond with exactly one word:
                Factual or CollectionEvidence.
                """),

            new(ChatRole.User, claim)
        };

        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        string classification = response.Text.Trim();

        if (Enum.TryParse<ClaimType>(classification, ignoreCase: true, out var claimType) && Enum.IsDefined(claimType))
        {
            Console.WriteLine($"CLAIM TYPE: {claimType} | {claim}");
            return claimType;
        }

        throw new InvalidOperationException($"Unexpected claim classification: {classification}");
    }
}
