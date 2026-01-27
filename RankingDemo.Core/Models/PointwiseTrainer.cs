using RankingDemo.Core.Data;
using RankingDemo.Core.Features;

namespace RankingDemo.Core.Models;

public static class PointwiseTrainer
{
    public static LinearScoringModel Train(
        IReadOnlyList<RankingRow> rows,
        int epochs = 30,
        float learningRate = 0.01f)
    {
        if (rows.Count == 0) throw new ArgumentException("Nincs tanító adat.");

       
        var x0 = FeatureExtractor.Extract(rows[0]);
        var model = new LinearScoringModel(x0.Length);

        
        for (int e = 0; e < epochs; e++)
        {
            foreach (var r in rows)
            {
                var x = FeatureExtractor.Extract(r);
                var y = r.Relevance; 

                var pred = model.Score(x);
                var err = pred - y; 

           
                for (int i = 0; i < model.Weights.Length; i++)
                {
                    model.Weights[i] -= learningRate * err * x[i];
                }
            }
        }

        return model;
    }
}
