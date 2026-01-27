using RankingDemo.Core.Data;
using RankingDemo.Core.Features;

namespace RankingDemo.Core.Models;

public static class PairwiseTrainer
{
    public static LinearScoringModel Train(
        IReadOnlyList<RankingRow> rows,
        int epochs = 30,
        float learningRate = 0.01f)
    {
        if (rows.Count < 2)
            throw new ArgumentException("Pairwise tanításhoz legalább 2 dokumentum kell.");

        var x0 = FeatureExtractor.Extract(rows[0]);
        var model = new LinearScoringModel(x0.Length);


        var pairs = new List<(RankingRow better, RankingRow worse)>();

        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = 0; j < rows.Count; j++)
            {
                if (rows[i].Relevance > rows[j].Relevance)
                    pairs.Add((rows[i], rows[j]));
            }
        }

        for (int e = 0; e < epochs; e++)
        {
            foreach (var (better, worse) in pairs)
            {
                var xb = FeatureExtractor.Extract(better);
                var xw = FeatureExtractor.Extract(worse);

                var sb = model.Score(xb);
                var sw = model.Score(xw);

                var margin = sb - sw;

                if (margin < 1f)
                {
                    for (int k = 0; k < model.Weights.Length; k++)
                    {
                        model.Weights[k] += learningRate * (xb[k] - xw[k]);
                    }
                }
            }
        }

        return model;
    }
}
