using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public sealed class FakeChatClient : IChatClient
{
    private readonly string _response;

    public FakeChatClient(string response)
    {
        _response = response;
    }

    public ChatClientMetadata Metadata => new("FakeChatClient");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _response)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming is not supported by FakeChatClient.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
    }
}