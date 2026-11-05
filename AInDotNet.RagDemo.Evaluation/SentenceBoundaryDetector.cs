using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Evaluation;

public static class SentenceBoundaryDetector
{
    private static readonly HashSet<string> TitleAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dr",
        "Mr",
        "Mrs",
        "Ms",
        "Prof"
    };

    public static bool IsSentenceBoundary(string text, int periodIndex)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (periodIndex < 0 || periodIndex >= text.Length)
            return false;

        if (text[periodIndex] != '.')
            return false;

        if (periodIndex + 1 >= text.Length)
            return false;

        if (!char.IsWhiteSpace(text[periodIndex + 1]))
            return false;

        // A period following a single-letter initial is not
        // sufficient evidence of a sentence boundary.
        if (periodIndex > 0 && char.IsLetter(text[periodIndex - 1]))
        {
            int start = periodIndex - 1;

            while (start > 0 && char.IsLetter(text[start - 1]))
                start--;

            if (periodIndex - start == 1)
                return false;

            string precedingWord = text[start..periodIndex];

            if (TitleAbbreviations.Contains(precedingWord))
                return false;
        }

        return true;
    }
}