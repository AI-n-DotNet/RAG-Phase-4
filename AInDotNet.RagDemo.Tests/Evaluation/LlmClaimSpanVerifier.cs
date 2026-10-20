using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class LlmClaimSpanVerifier : IClaimSpanVerifier
{
    private readonly IChatClient _chatClient;

    public LlmClaimSpanVerifier(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<bool> VerifyAsync(string claim, string originalText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claim);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalText);

        const string instructions = """
            You are verifying whether an extracted factual claim is supported by its original answer passage.

            Determine whether the passage explicitly states or logically entails the claim.

            Do not use outside knowledge.
            Ignore citation markers such as [S1] and [S5].
            Do not infer facts that are not established by the passage.

            QUALIFICATION AND SCOPE PRESERVATION:

            An extracted claim must preserve the original passage's meaning, qualifications, uncertainty, and negation.

            Distinguish between:
            - A passage not establishing that something is implemented.
            - A passage establishing that something is not implemented.

            These are NOT equivalent.

            Statements such as "not confirmed", "not specified", "not documented", or "not established by the sources" must not be converted into assertions of nonimplementation or nonexistence.

            If an extracted claim removes a qualification and thereby makes a stronger assertion than the original passage, return UNSUPPORTED.

            When the passage is ambiguous, do not infer the stronger claim.

            CRITICAL: DOCUMENTATION SCOPE IS NOT IMPLEMENTATION STATUS

            A statement that a technology is discussed generally, but is not specifically applied to a particular application in the passage, does not establish whether that technology is actually implemented in the application.

            Example:

            Original passage:
            "OAuth 2.0 is noted as a framework for delegated authorization generally but not specifically applied to your RAG demo app."

            Extracted claim:
            "OAuth 2.0 is not implemented in the RAG demo application."

            Correct verdict: UNSUPPORTED.

            Reason:
            The passage does not establish the application's implementation status. The technology might be implemented even though the passage does not specifically describe its application.

            Do not interpret "not specifically applied", "not discussed for this application", or "not documented as implemented" as proof of nonimplementation.

            Before returning SUPPORTED, verify that the original passage necessarily establishes the claim AS WRITTEN.

            Respond with exactly one word:
            SUPPORTED or UNSUPPORTED.
            """;

        string prompt = $"""
            CLAIM:
            {claim}

            ORIGINAL ANSWER PASSAGE:
            {originalText}

                    
            VERIFICATION QUESTION:

            Does the original passage necessarily establish the extracted claim?

            Consider whether the passage could be true while the extracted claim is false.

            If that is possible, return UNSUPPORTED.

            If the passage necessarily establishes the claim, return SUPPORTED.
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, instructions),
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        string verdict = response.Text.Trim();

        return verdict switch
        {
            "SUPPORTED" => true,
            "UNSUPPORTED" => false,
            _ => throw new InvalidOperationException($"Unexpected claim-span verification response: {verdict}")
        };
    }
}