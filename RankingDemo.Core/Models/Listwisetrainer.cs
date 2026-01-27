using RankingDemo.Core.Data;
using RankingDemo.Core.Features;

namespace RankingDemo.Core.Models;

public static class ListwiseTrainer
{
    public static LinearScoringModel Train(
        IReadOnlyList<RankingRow> rows,
        int epochs = 50,
        float learningRate = 0.01f)
    {
        if (rows.Count < 2)
            throw new ArgumentException("Listwise tanításhoz legalább 2 dokumentum szükséges.");

        var x0 = FeatureExtractor.Extract(rows[0]);
        var model = new LinearScoringModel(x0.Length);

        for (int e = 0; e < epochs; e++)
        {
            
            var scores = rows.Select(r =>
            {
                var x = FeatureExtractor.Extract(r);
                return model.Score(x);
            }).ToArray();

            
            var max = scores.Max();
            var exp = scores.Select(s => MathF.Exp(s - max)).ToArray();
            var sumExp = exp.Sum();
            var probs = exp.Select(v => v / sumExp).ToArray();

         
            var relSum = rows.Sum(r => r.Relevance);
            if (relSum == 0) continue;

            for (int i = 0; i < rows.Count; i++)
            {
                var targetProb = rows[i].Relevance / (float)relSum;
                var error = probs[i] - targetProb;

                var x = FeatureExtractor.Extract(rows[i]);
                for (int k = 0; k < model.Weights.Length; k++)
                {
                    model.Weights[k] -= learningRate * error * x[k];
                }
            }
        }

        return model;
    }
}
