using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AInDotNet.RagDemo.Application.Generation;

public static class CitationProcessor
{
    private static readonly Regex CitationPattern = new(@"\[(?:S\d+)(?:\s*,\s*S\d+)*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LabelPattern = new(@"S\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static RagAnswerResult Process(string answer, IReadOnlyList<RagSource> sources)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(sources);

        var sourceLookup = sources.ToDictionary(source => source.Label, StringComparer.Ordinal);

        var citedLabels = new HashSet<string>(StringComparer.Ordinal);

        string normalizedAnswer = CitationPattern.Replace(answer,
            match =>
            {
                var validLabels = LabelPattern.Matches(match.Value)
                    .Select(m => m.Value)
                    .Where(sourceLookup.ContainsKey)
                    .Distinct()
                    .ToArray();

                foreach (string label in validLabels)
                    citedLabels.Add(label);

                return validLabels.Length == 0
                    ? string.Empty
                    : $"[{string.Join(", ", validLabels)}]";
            });

        var citedSources = sources.Where(source => citedLabels.Contains(source.Label)).ToArray();

        return new RagAnswerResult(normalizedAnswer, citedSources, sources);
    }
}