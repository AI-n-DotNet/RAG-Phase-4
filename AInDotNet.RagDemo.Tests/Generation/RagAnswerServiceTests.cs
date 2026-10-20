using System;
using System.Collections.Generic;
using System.Text;
using AInDotNet.RagDemo.Application.Generation;
using AInDotNet.RagDemo.Application.Retrieval;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Tests.Generation;


[TestClass]
public sealed class RagAnswerServiceTests
{
    [TestMethod]
    public async Task AnswerAsync_NoMatches_ReturnsAbstentionWithoutCallingChat()
    {
        using var chatClient = new UnexpectedCallChatClient();

        var service = new RagAnswerService(new EmptySearchService(), chatClient);
        RagAnswerResult result = await service.AnswerAsync("Where are document embeddings stored?");

        Assert.AreEqual("I couldn't find relevant information in the available documents.", result.Answer);

        Assert.AreEqual(0, result.Sources.Count);
        Assert.AreEqual(0, result.RetrievedSources.Count);
    }

    private sealed class EmptySearchService : ISemanticSearchService
    {
        public Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
        }
    }

    private sealed class UnexpectedCallChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The chat model must not be called when retrieval is empty.");
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Streaming must not be called when retrieval is empty.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}