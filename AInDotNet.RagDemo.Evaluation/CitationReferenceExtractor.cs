using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AInDotNet.RagDemo.Evaluation;


public static class CitationReferenceExtractor
{
    private static readonly Regex CitationPattern = new(@"\[(?:(?i:Source)[ \t]+)?S\d+(?:\s*,\s*(?:(?i:Source)[ \t]+)?S\d+)*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex LabelPattern = new(@"S\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> Extract(string answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return CitationPattern.Matches(answer)
            .SelectMany(match => LabelPattern.Matches(match.Value).Select(label => label.Value))
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