using RankingDemo.Core.Data;

namespace RankingDemo.Core.Evaluation;

public static class Ndcg
{
    public static float Compute(IReadOnlyList<RankingRow> rankedRows, int k)
    {
        if (rankedRows.Count == 0 || k <= 0)
            return 0f;

        var dcg = Dcg(rankedRows, k);

        var ideal = rankedRows
            .OrderByDescending(r => r.Relevance)
            .ToList();

        var idcg = Dcg(ideal, k);

        if (idcg == 0f) return 0f;
        return dcg / idcg;
    }

    private static float Dcg(IReadOnlyList<RankingRow> rows, int k)
    {
        int take = Math.Min(k, rows.Count);
        float sum = 0f;

        for (int i = 0; i < take; i++)
        {
            var rel = rows[i].Relevance;
            var denom = MathF.Log2(i + 2);
            sum += (MathF.Pow(2, rel) - 1) / denom;
        }

        return sum;
    }
}
