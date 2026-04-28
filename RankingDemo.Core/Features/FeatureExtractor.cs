using System.Text.RegularExpressions;
using RankingDemo.Core.Data;

namespace RankingDemo.Core.Features;

public static class FeatureExtractor
{
    public static float[] Extract(RankingRow row)
    {
        var qTokens = Tokenize(row.QueryText);
        var dTokens = Tokenize(row.DocText);
        var hasStructuredStats =
            row.Points.HasValue ||
            row.GoalDifference.HasValue ||
            row.Wins.HasValue ||
            row.GoalsFor.HasValue ||
            row.GoalsAgainst.HasValue;

        float hits = 0f;
        float coverage = 0f;

        if (qTokens.Count > 0)
        {
            int covered = 0;
            var dSet = new HashSet<string>(dTokens);

            foreach (var qt in qTokens)
            {
                if (dSet.Contains(qt))
                {
                    covered++;
                }
            }

            var qSet = new HashSet<string>(qTokens);
            foreach (var dt in dTokens)
            {
                if (qSet.Contains(dt))
                {
                    hits++;
                }
            }

            coverage = (float)covered / qSet.Count;
        }

        return
        [
            Normalize(row.Points, 100f, hasStructuredStats),
            Normalize(row.GoalDifference, 50f, hasStructuredStats),
            Normalize(row.Wins, 38f, hasStructuredStats),
            Normalize(row.GoalsFor, 100f, hasStructuredStats),
            NormalizeNegative(row.GoalsAgainst, 100f, hasStructuredStats),
            hits / 20f,
            LogLen(row.DocText),
            coverage
        ];
    }

    private static float Normalize(int? value, float scale, bool enabled)
    {
        if (!enabled || !value.HasValue)
        {
            return 0f;
        }

        return value.Value / scale;
    }

    private static float NormalizeNegative(int? value, float scale, bool enabled)
    {
        return -Normalize(value, scale, enabled);
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
