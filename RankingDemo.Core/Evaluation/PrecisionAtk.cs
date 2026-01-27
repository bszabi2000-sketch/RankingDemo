using RankingDemo.Core.Data;

namespace RankingDemo.Core.Evaluation;

public static class PrecisionAtK
{
    public static float Compute(
        IReadOnlyList<RankingRow> rankedRows,
        int k,
        int relevanceThreshold = 1)
    {
        if (rankedRows.Count == 0 || k <= 0)
            return 0f;

        int take = Math.Min(k, rankedRows.Count);

        int relevant = rankedRows
            .Take(take)
            .Count(r => r.Relevance >= relevanceThreshold);

        return relevant / (float)take;
    }
}
