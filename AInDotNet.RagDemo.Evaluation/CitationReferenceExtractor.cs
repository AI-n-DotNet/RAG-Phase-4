using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AInDotNet.RagDemo.Evaluation;


public static class CitationReferenceExtractor
{
    private static readonly Regex CitationPattern = new(        @"\[S\d+(?:\s*,\s*S\d+)*\]",        RegexOptions.Compiled);

    public static IReadOnlyList<string> Extract(string answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return CitationPattern.Matches(answer)
            .SelectMany(match =>
                match.Value.Trim('[', ']')
                    .Split(',', StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static string RemoveMarkers(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!CitationPattern.IsMatch(text))
            return text;

        string withoutMarkers = CitationPattern.Replace(text, string.Empty);
        return Regex.Replace(withoutMarkers, @"[ \t]+(?=[.,;:!?])", string.Empty).Trim();
    }
}