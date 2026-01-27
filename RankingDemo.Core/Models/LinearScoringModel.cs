namespace RankingDemo.Core.Models;

public sealed class LinearScoringModel
{
    public float[] Weights { get; }

    public LinearScoringModel(int featureCount)
    {
        Weights = new float[featureCount];
        
    }

    public float Score(float[] x)
    {
        float s = 0f;
        for (int i = 0; i < Weights.Length; i++)
            s += Weights[i] * x[i];
        return s;
    }
}
