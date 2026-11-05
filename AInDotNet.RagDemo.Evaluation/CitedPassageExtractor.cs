using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AInDotNet.RagDemo.Evaluation;


public static class CitedPassageExtractor
{
    public static IReadOnlyList<CitedPassage> Extract(string answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var passages = new List<CitedPassage>();

        using var reader = new StringReader(answer);

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Find sentence boundaries in the line and split it into segments
            var sentenceSegments = new List<string>();

            int segmentStart = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (!SentenceBoundaryDetector.IsSentenceBoundary(line, i))
                    continue;

                int nextStart = i + 1;

                while (nextStart < line.Length && char.IsWhiteSpace(line[nextStart]))
                {
                    nextStart++;
                }

                if (nextStart >= line.Length)
                    continue;

                sentenceSegments.Add(line[segmentStart..(i + 1)]);
                segmentStart = nextStart;
            }

            sentenceSegments.Add(line[segmentStart..]);

            foreach (string sentence in sentenceSegments)
            {
                string[] segments = Regex.Split(sentence, @"(?<=\]),\s+(?=and\s|but\s)", RegexOptions.IgnoreCase);

                foreach (string segment in segments)
                {
                    if (string.IsNullOrWhiteSpace(segment))
                        continue;

                    passages.Add(new CitedPassage(
                        PassageId: passages.Count + 1,
                        OriginalText: segment,
                        CitationLabels: CitationReferenceExtractor.Extract(segment)
                            .Distinct(StringComparer.Ordinal)
                            .ToArray())
                    {
                        ContextText = sentence
                    });
                }
            }
        }

        foreach (var passage in passages)
        {
            Console.WriteLine($"PASSAGE {passage.PassageId}: {passage.OriginalText}");
            Console.WriteLine($"CITATIONS: {string.Join(", ", passage.CitationLabels)}");
        }

        return passages;
    }
}