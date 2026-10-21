using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Benchmarks;

public static class CosineSimilarity
{
    public static double Calculate(float[] left, float[] right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Length == 0 || left.Length != right.Length)
        {
            throw new ArgumentException("Vectors must have equal, nonzero dimensions.");
        }

        // Accumulate in double precision while storing vectors as floats.
        double dotProduct = 0;
        double leftSquaredMagnitude = 0;
        double rightSquaredMagnitude = 0;

        for (int index = 0; index < left.Length; index++)
        {
            double a = left[index];
            double b = right[index];

            if (!double.IsFinite(a) || !double.IsFinite(b))
            {
                throw new ArgumentException("Vector values must be finite.");
            }

            dotProduct += a * b;
            leftSquaredMagnitude += a * a;
            rightSquaredMagnitude += b * b;
        }

        if (leftSquaredMagnitude == 0 || rightSquaredMagnitude == 0)
        {
            throw new ArgumentException("Cosine similarity is undefined for a zero vector.");
        }

        return dotProduct / (Math.Sqrt(leftSquaredMagnitude) * Math.Sqrt(rightSquaredMagnitude));
    }
}