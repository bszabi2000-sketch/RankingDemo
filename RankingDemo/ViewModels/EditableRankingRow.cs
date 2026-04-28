using CommunityToolkit.Mvvm.ComponentModel;
using RankingDemo.Core.Data;

namespace RankingDemo.Wpf.ViewModels;

public partial class EditableRankingRow : ObservableObject
{
    [ObservableProperty] private string _queryId = "Q1";
    [ObservableProperty] private string _queryText = "Melyik csapat kerüljön előrébb a rangsorban?";
    [ObservableProperty] private string _docId = string.Empty;
    [ObservableProperty] private string _docText = string.Empty;
    [ObservableProperty] private int _relevance;
    [ObservableProperty] private int? _points;
    [ObservableProperty] private int? _goalDifference;
    [ObservableProperty] private int? _wins;
    [ObservableProperty] private int? _goalsFor;
    [ObservableProperty] private int? _goalsAgainst;

    public RankingRow ToRankingRow()
    {
        var queryId = string.IsNullOrWhiteSpace(QueryId) ? "Q1" : QueryId.Trim();
        var queryText = string.IsNullOrWhiteSpace(QueryText)
            ? "Melyik csapat kerüljön előrébb a rangsorban?"
            : QueryText.Trim();
        var docId = string.IsNullOrWhiteSpace(DocId) ? "Ismeretlen csapat" : DocId.Trim();
        var docText = string.IsNullOrWhiteSpace(DocText) ? BuildDocText(docId) : DocText.Trim();

        return new RankingRow(
            queryId,
            queryText,
            docId,
            docText,
            Relevance,
            Points,
            GoalDifference,
            Wins,
            GoalsFor,
            GoalsAgainst);
    }

    public static EditableRankingRow FromRankingRow(RankingRow row)
    {
        return new EditableRankingRow
        {
            QueryId = row.QueryId,
            QueryText = row.QueryText,
            DocId = row.DocId,
            DocText = row.DocText,
            Relevance = row.Relevance,
            Points = row.Points,
            GoalDifference = row.GoalDifference,
            Wins = row.Wins,
            GoalsFor = row.GoalsFor,
            GoalsAgainst = row.GoalsAgainst
        };
    }

    private string BuildDocText(string docId)
    {
        var parts = new List<string> { docId };

        if (Points.HasValue)
        {
            parts.Add($"{Points.Value} pont");
        }

        if (Wins.HasValue)
        {
            parts.Add($"{Wins.Value} győzelem");
        }

        if (GoalDifference.HasValue)
        {
            parts.Add($"{GoalDifference.Value} gólkülönbség");
        }

        if (GoalsFor.HasValue && GoalsAgainst.HasValue)
        {
            parts.Add($"{GoalsFor.Value}:{GoalsAgainst.Value} gólarány");
        }

        return string.Join(", ", parts);
    }
}
