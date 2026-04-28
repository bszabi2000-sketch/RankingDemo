namespace RankingDemo.Core.Data;

public sealed record RankingRow(
    string QueryId,
    string QueryText,
    string DocId,
    string DocText,
    int Relevance,
    int? Points = null,
    int? GoalDifference = null,
    int? Wins = null,
    int? GoalsFor = null,
    int? GoalsAgainst = null);
