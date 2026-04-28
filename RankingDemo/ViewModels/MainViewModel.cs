using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RankingDemo.Core.Data;
using RankingDemo.Core.Evaluation;
using RankingDemo.Core.Features;
using RankingDemo.Core.Models;
using System.Collections.ObjectModel;

namespace RankingDemo.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public sealed record RankedRow(
        int Position,
        string DocId,
        float Score,
        int Relevance,
        int? Points,
        int? GoalDifference,
        int? Wins,
        string DocText);

    public sealed record MetricResultRow(string Algorithm, float PrecisionAtK, float NdcgAtK);

    [ObservableProperty] private ObservableCollection<MetricResultRow> _metricResults = new();
    [ObservableProperty] private ObservableCollection<string> _queries = new();
    [ObservableProperty] private string? _selectedQuery;
    [ObservableProperty] private ObservableCollection<RankingRow> _rowsForSelectedQuery = new();
    [ObservableProperty] private ObservableCollection<EditableRankingRow> _manualRows = new();
    [ObservableProperty] private EditableRankingRow? _selectedManualRow;
    [ObservableProperty] private ObservableCollection<RankedRow> _pointwiseRankedRows = new();
    [ObservableProperty] private ObservableCollection<RankedRow> _pairwiseRankedRows = new();
    [ObservableProperty] private ObservableCollection<RankedRow> _listwiseRankedRows = new();
    [ObservableProperty] private int _k = 3;
    [ObservableProperty] private string _statusMessage = "Tölts be vagy adj meg adatokat.";

    private List<RankingRow> _allRows = [];

    public MainViewModel()
    {
        LoadFootballSampleRows();
        ApplyManualData();
    }

    [RelayCommand]
    private void LoadCsv()
    {
        var dlg = new OpenFileDialog
        {
            Title = "CSV kiválasztása",
            Filter = "CSV fájl (*.csv)|*.csv|Minden fájl (*.*)|*.*"
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        SetActiveRows(CsvDatasetLoader.Load(dlg.FileName));
        ManualRows = new ObservableCollection<EditableRankingRow>(_allRows.Select(EditableRankingRow.FromRankingRow));
        StatusMessage = $"CSV betöltve: {_allRows.Count} sor, {Queries.Count} lekérdezés.";
    }

    [RelayCommand]
    private void LoadFootballSample()
    {
        LoadFootballSampleRows();
        ApplyManualData();
    }

    [RelayCommand]
    private void AddManualRow()
    {
        var template = new EditableRankingRow();

        if (ManualRows.Count > 0)
        {
            template.QueryId = ManualRows[0].QueryId;
            template.QueryText = ManualRows[0].QueryText;
        }

        ManualRows.Add(template);
        SelectedManualRow = template;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveManualRow))]
    private void RemoveSelectedManualRow()
    {
        if (SelectedManualRow is null)
        {
            return;
        }

        ManualRows.Remove(SelectedManualRow);
        SelectedManualRow = null;
    }

    private bool CanRemoveManualRow()
    {
        return SelectedManualRow is not null;
    }

    [RelayCommand]
    private void ApplyManualData()
    {
        var rows = ManualRows
            .Select(r => r.ToRankingRow())
            .Where(IsValidRow)
            .ToList();

        SetActiveRows(rows);
        StatusMessage = $"Kézi adat aktiválva: {_allRows.Count} sor, {Queries.Count} lekérdezés.";
    }

    [RelayCommand]
    private void CompareAlgorithms()
    {
        MetricResults.Clear();
        PointwiseRankedRows.Clear();
        PairwiseRankedRows.Clear();
        ListwiseRankedRows.Clear();

        if (_allRows.Count == 0 || string.IsNullOrWhiteSpace(SelectedQuery))
        {
            StatusMessage = "Nincs aktív adatsor vagy nincs kiválasztott lekérdezés.";
            return;
        }

        var queryId = ExtractQueryId(SelectedQuery);
        var rows = _allRows.Where(r => r.QueryId == queryId).ToList();

        if (rows.Count < 2)
        {
            StatusMessage = "Az összehasonlításhoz legalább 2 sor kell ugyanahhoz a lekérdezéshez.";
            return;
        }

        var pointwiseModel = PointwiseTrainer.Train(rows, epochs: 50, learningRate: 0.01f);
        var pairwiseModel = PairwiseTrainer.Train(rows, epochs: 50, learningRate: 0.01f);
        var listwiseModel = ListwiseTrainer.Train(rows, epochs: 80, learningRate: 0.02f);

        var pointwiseRows = CreateRankedRows(pointwiseModel, rows);
        var pairwiseRows = CreateRankedRows(pairwiseModel, rows);
        var listwiseRows = CreateRankedRows(listwiseModel, rows);

        PointwiseRankedRows = new ObservableCollection<RankedRow>(pointwiseRows);
        PairwiseRankedRows = new ObservableCollection<RankedRow>(pairwiseRows);
        ListwiseRankedRows = new ObservableCollection<RankedRow>(listwiseRows);

        MetricResults.Add(EvaluateAlgorithm("Pointwise", pointwiseRows, queryId));
        MetricResults.Add(EvaluateAlgorithm("Pairwise", pairwiseRows, queryId));
        MetricResults.Add(EvaluateAlgorithm("Listwise", listwiseRows, queryId));

        StatusMessage = $"Összehasonlítás elkészült a(z) {queryId} lekérdezésre.";
    }

    partial void OnSelectedQueryChanged(string? value)
    {
        RefreshRows();
    }

    partial void OnSelectedManualRowChanged(EditableRankingRow? value)
    {
        RemoveSelectedManualRowCommand.NotifyCanExecuteChanged();
    }

    private void SetActiveRows(IEnumerable<RankingRow> rows)
    {
        _allRows = rows.ToList();

        var distinctQueries = _allRows
            .Select(r => $"{r.QueryId} — {r.QueryText}")
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        Queries = new ObservableCollection<string>(distinctQueries);

        if (SelectedQuery is null || !Queries.Contains(SelectedQuery))
        {
            SelectedQuery = Queries.FirstOrDefault();
        }

        RefreshRows();
        MetricResults.Clear();
        PointwiseRankedRows.Clear();
        PairwiseRankedRows.Clear();
        ListwiseRankedRows.Clear();
    }

    private void RefreshRows()
    {
        RowsForSelectedQuery.Clear();

        if (string.IsNullOrWhiteSpace(SelectedQuery))
        {
            return;
        }

        var queryId = ExtractQueryId(SelectedQuery);

        foreach (var row in _allRows
                     .Where(r => r.QueryId == queryId)
                     .OrderByDescending(r => r.Relevance)
                     .ThenByDescending(r => r.Points ?? int.MinValue)
                     .ThenByDescending(r => r.GoalDifference ?? int.MinValue))
        {
            RowsForSelectedQuery.Add(row);
        }
    }

    private static string ExtractQueryId(string selectedQuery)
    {
        return selectedQuery.Split('—')[0].Trim();
    }

    private static bool IsValidRow(RankingRow row)
    {
        return !string.IsNullOrWhiteSpace(row.QueryId) &&
               !string.IsNullOrWhiteSpace(row.QueryText) &&
               !string.IsNullOrWhiteSpace(row.DocId);
    }

    private List<RankedRow> CreateRankedRows(LinearScoringModel model, List<RankingRow> rows)
    {
        return rows
            .Select(r =>
            {
                var x = FeatureExtractor.Extract(r);
                return new RankedRow(
                    Position: 0,
                    DocId: r.DocId,
                    Score: model.Score(x),
                    Relevance: r.Relevance,
                    Points: r.Points,
                    GoalDifference: r.GoalDifference,
                    Wins: r.Wins,
                    DocText: r.DocText);
            })
            .OrderByDescending(r => r.Score)
            .Select((r, index) => r with { Position = index + 1 })
            .ToList();
    }

    private MetricResultRow EvaluateAlgorithm(string algorithmName, IReadOnlyList<RankedRow> rankedRows, string queryId)
    {
        var rankedAsRows = rankedRows
            .Select(rr => new RankingRow(
                queryId,
                SelectedQuery ?? string.Empty,
                rr.DocId,
                rr.DocText,
                rr.Relevance,
                rr.Points,
                rr.GoalDifference,
                rr.Wins))
            .ToList();

        return new MetricResultRow(
            algorithmName,
            PrecisionAtK.Compute(rankedAsRows, K),
            Ndcg.Compute(rankedAsRows, K));
    }

    private void LoadFootballSampleRows()
    {
        ManualRows = new ObservableCollection<EditableRankingRow>(
        [
            new()
            {
                QueryId = "FOCI-01",
                QueryText = "Melyik csapat kerüljön előrébb a szakértői erősorrendben?",
                DocId = "Budai FC",
                Relevance = 5,
                Points = 72,
                GoalDifference = 18,
                Wins = 22,
                GoalsFor = 60,
                GoalsAgainst = 42,
                DocText = "Stabil védekezés, kevés vereség, megbízható csapatjáték."
            },
            new()
            {
                QueryId = "FOCI-01",
                QueryText = "Melyik csapat kerüljön előrébb a szakértői erősorrendben?",
                DocId = "Duna United",
                Relevance = 4,
                Points = 70,
                GoalDifference = 29,
                Wins = 20,
                GoalsFor = 69,
                GoalsAgainst = 40,
                DocText = "Látványos támadófutball, magas gólkülönbség, kissé ingadozó forma."
            },
            new()
            {
                QueryId = "FOCI-01",
                QueryText = "Melyik csapat kerüljön előrébb a szakértői erősorrendben?",
                DocId = "Alföld SC",
                Relevance = 3,
                Points = 74,
                GoalDifference = 10,
                Wins = 21,
                GoalsFor = 55,
                GoalsAgainst = 45,
                DocText = "Sok pontot gyűjtött, de szűk meccseken nyert és kevésbé domináns."
            },
            new()
            {
                QueryId = "FOCI-01",
                QueryText = "Melyik csapat kerüljön előrébb a szakértői erősorrendben?",
                DocId = "Tisza TK",
                Relevance = 2,
                Points = 67,
                GoalDifference = 31,
                Wins = 19,
                GoalsFor = 72,
                GoalsAgainst = 41,
                DocText = "Erős támadósor, nagy gólkülönbség, de pontvesztések a rangadókon."
            },
            new()
            {
                QueryId = "FOCI-01",
                QueryText = "Melyik csapat kerüljön előrébb a szakértői erősorrendben?",
                DocId = "Hegyvidék SE",
                Relevance = 1,
                Points = 69,
                GoalDifference = 8,
                Wins = 18,
                GoalsFor = 50,
                GoalsAgainst = 42,
                DocText = "Szervezett csapat, kevés kiugró mutatóval."
            },
            new()
            {
                QueryId = "FOCI-01",
                QueryText = "Melyik csapat kerüljön előrébb a szakértői erősorrendben?",
                DocId = "Maros VK",
                Relevance = 0,
                Points = 63,
                GoalDifference = 4,
                Wins = 17,
                GoalsFor = 48,
                GoalsAgainst = 44,
                DocText = "Masszív középcsapat, kevés extra teljesítménnyel."
            }
        ]);
    }
}
