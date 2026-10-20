using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Application.Ingestion;


public sealed record TextFileDocument(string Title, string SourceUri, string Content)
{
    public static async Task<TextFileDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);

        if (!string.Equals(Path.GetExtension(fullPath), ".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Select a plain-text (.txt) file.", nameof(path));
        }

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("The selected file contains no text.", nameof(path));
        }

        // The ingestion service uses SourceUri to identify documents.
        return new TextFileDocument(Path.GetFileNameWithoutExtension(fullPath), new Uri(fullPath).AbsoluteUri, content);
    }
}