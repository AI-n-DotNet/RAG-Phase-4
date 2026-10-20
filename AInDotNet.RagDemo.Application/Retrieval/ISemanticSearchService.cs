using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Application.Retrieval;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}

public sealed record SearchResult(int ChunkId, int DocumentId, string DocumentTitle, string Content, double Distance);
