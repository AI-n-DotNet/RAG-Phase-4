using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AInDotNet.RagDemo.Tests.Evaluation;


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
}