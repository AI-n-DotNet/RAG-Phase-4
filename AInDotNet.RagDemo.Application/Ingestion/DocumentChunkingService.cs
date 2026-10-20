using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Application.Ingestion;

public sealed class DocumentChunkingService
{
    public IReadOnlyList<TextChunk> ChunkDocument(string text, int maxChunkSize = 1500, int overlapSize = 200)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (maxChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize));

        if (overlapSize < 0 || overlapSize >= maxChunkSize)
            throw new ArgumentOutOfRangeException(nameof(overlapSize), "Overlap must be smaller than chunk size.");

        if (string.IsNullOrWhiteSpace(text))
            return [];

        // Normalize line endings, preserving paragraph boundaries.
        text = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

        var chunks = new List<TextChunk>();
        int position = 0;

        while (position < text.Length)
        {
            int remaining = text.Length - position;
            int length = Math.Min(maxChunkSize, remaining);

            int end = position + length;

            // Prefer ending at a paragraph boundary.
            if (end < text.Length)
            {
                int minimumBoundary = position + maxChunkSize / 2;

                int paragraphEnd = text.LastIndexOf("\n\n", end - 1, length, StringComparison.Ordinal);

                if (paragraphEnd >= minimumBoundary)
                {
                    end = paragraphEnd + 2;
                }
                else
                {
                    // Otherwise prefer a sentence boundary.
                    int sentenceEnd = text.LastIndexOf(". ", end - 1, length, StringComparison.Ordinal);

                    if (sentenceEnd >= minimumBoundary)
                        end = sentenceEnd + 1;
                }
            }

            string content = text[position..end].Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(new TextChunk(chunks.Count, content, position, end));
            }

            if (end >= text.Length)
                break;

            // Move backward to retain context from the prior chunk.
            int nextPosition = end - overlapSize;

            // Prefer starting at a word boundary.
            if (nextPosition > position)
            {
                while (nextPosition < end && !char.IsWhiteSpace(text[nextPosition - 1]))
                {
                    nextPosition++;
                }

                // Skip whitespace before the next word.
                while (nextPosition < end && char.IsWhiteSpace(text[nextPosition]))
                {
                    nextPosition++;
                }
            }

            // Guarantee forward progress.
            position = nextPosition < end ? Math.Max(position + 1, nextPosition) : end;
        }

        return chunks;
    }
}

public sealed record TextChunk(int ChunkIndex, string Content, int StartOffset, int EndOffset);
