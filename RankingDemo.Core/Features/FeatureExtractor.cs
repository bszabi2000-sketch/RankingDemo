using System.Text.RegularExpressions;
using RankingDemo.Core.Data;

namespace RankingDemo.Core.Features;

public static class FeatureExtractor
{
    public static float[] Extract(RankingRow row)
    {
        var qTokens = Tokenize(row.QueryText);
        var dTokens = Tokenize(row.DocText);

        if (qTokens.Count == 0)
            return new float[] { 0f, LogLen(row.DocText), 0f };

        int hits = 0;
        int covered = 0;

        var dSet = new HashSet<string>(dTokens);

        foreach (var qt in qTokens)
        {
            if (dSet.Contains(qt)) covered++;
        }


        var qSet = new HashSet<string>(qTokens);
        foreach (var dt in dTokens)
        {
            if (qSet.Contains(dt)) hits++;
        }

        float coverage = (float)covered / qSet.Count;

        return new float[]
        {
            hits,
            LogLen(row.DocText),
            coverage
        };
    }

    private static float LogLen(string text)
    {
        var n = string.IsNullOrWhiteSpace(text) ? 0 : text.Length;
        return (float)Math.Log(1 + n);
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        
        var matches = Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}]+");
        return matches.Select(m => m.Value).ToList();
    }
}
