using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RankingDemo.Core.Data;
using RankingDemo.Core.Features;
using RankingDemo.Core.Models;
using System.Collections.ObjectModel;
using RankingDemo.Core.Evaluation;


namespace RankingDemo.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public sealed record RankedRow(string DocId, float Score, int Relevance, string DocText);
    #region Propertys

    
    [ObservableProperty] private ObservableCollection<string> _queries = new();
    [ObservableProperty] private string? _selectedQuery;
    [ObservableProperty] private ObservableCollection<RankedRow> _rankedRows = new();
    [ObservableProperty] private float _precisionAt3;
    [ObservableProperty] private float _ndcgAt3;

    [ObservableProperty] private ObservableCollection<RankingRow> _rowsForSelectedQuery = new();

    private List<RankingRow> _allRows = new();

    [ObservableProperty]
    private ObservableCollection<RankingAlgorithm> _algorithms = new(
    Enum.GetValues(typeof(RankingAlgorithm)).Cast<RankingAlgorithm>());

    [ObservableProperty] private RankingAlgorithm _selectedAlgorithm = RankingAlgorithm.Pointwise;

    [ObservableProperty] private int _k = 3;

    [ObservableProperty] private float _precisionAtKValue;
    [ObservableProperty] private float _ndcgAtKValue;
    #endregion

    #region Commands


    [RelayCommand]
    private void TrainPointwise()
    {
        RankedRows.Clear();

        if (_allRows.Count == 0 || string.IsNullOrWhiteSpace(SelectedQuery))
            return;

        var queryId = SelectedQuery.Split('—')[0].Trim();
        var rows = _allRows.Where(r => r.QueryId == queryId).ToList();
        if (rows.Count == 0) return;

        var model = PointwiseTrainer.Train(rows, epochs: 50, learningRate: 0.01f);

        var ranked = rows
            .Select(r =>
            {
                var x = FeatureExtractor.Extract(r);
                var score = model.Score(x);
                return new RankedRow(r.DocId, score, r.Relevance, r.DocText);
            })
            .OrderByDescending(rr => rr.Score)
            .ToList();


        foreach (var rr in ranked)
            RankedRows.Add(rr);

 
        PrecisionAt3 = PrecisionAtK.Compute(
            rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankingRow(r.QueryId, r.QueryText, r.DocId, r.DocText, r.Relevance);
                })
                .OrderByDescending(r => model.Score(FeatureExtractor.Extract(r)))
                .ToList(),
            k: 3);

        NdcgAt3 = Ndcg.Compute(
            rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankingRow(r.QueryId, r.QueryText, r.DocId, r.DocText, r.Relevance);
                })
                .OrderByDescending(r => model.Score(FeatureExtractor.Extract(r)))
                .ToList(),
            k: 3);

    }

    [RelayCommand]
    private void LoadCsv()
    {
        var dlg = new OpenFileDialog
        {
            Title = "CSV kiválasztása",
            Filter = "CSV fájl (*.csv)|*.csv|Minden fájl (*.*)|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        _allRows = CsvDatasetLoader.Load(dlg.FileName);

        var distinctQueries = _allRows
            .Select(r => $"{r.QueryId} — {r.QueryText}")
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        Queries = new ObservableCollection<string>(distinctQueries);
        SelectedQuery = Queries.FirstOrDefault();

        RefreshRows();



    }

    [RelayCommand]
    private void TrainPairwise()
    {
        RankedRows.Clear();

        if (_allRows.Count == 0 || string.IsNullOrWhiteSpace(SelectedQuery))
            return;

        var queryId = SelectedQuery.Split('—')[0].Trim();
        var rows = _allRows.Where(r => r.QueryId == queryId).ToList();
        if (rows.Count < 2) return;

        var model = PairwiseTrainer.Train(rows, epochs: 50, learningRate: 0.01f);

        var ranked = rows
            .Select(r =>
            {
                var x = FeatureExtractor.Extract(r);
                var score = model.Score(x);
                return new RankedRow(r.DocId, score, r.Relevance, r.DocText);
            })
            .OrderByDescending(rr => rr.Score)
            .ToList();

        foreach (var rr in ranked)
            RankedRows.Add(rr);

    
        PrecisionAt3 = PrecisionAtK.Compute(
            rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankingRow(r.QueryId, r.QueryText, r.DocId, r.DocText, r.Relevance);
                })
                .OrderByDescending(r => model.Score(FeatureExtractor.Extract(r)))
                .ToList(),
            k: 3);

        NdcgAt3 = Ndcg.Compute(
            rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankingRow(r.QueryId, r.QueryText, r.DocId, r.DocText, r.Relevance);
                })
                .OrderByDescending(r => model.Score(FeatureExtractor.Extract(r)))
                .ToList(),
            k: 3);

    }

    [RelayCommand]
    private void TrainListwise()
    {
        RankedRows.Clear();

        if (_allRows.Count == 0 || string.IsNullOrWhiteSpace(SelectedQuery))
            return;

        var queryId = SelectedQuery.Split('—')[0].Trim();
        var rows = _allRows.Where(r => r.QueryId == queryId).ToList();
        if (rows.Count < 2) return;

        var model = ListwiseTrainer.Train(rows, epochs: 80, learningRate: 0.02f);

        var ranked = rows
            .Select(r =>
            {
                var x = FeatureExtractor.Extract(r);
                var score = model.Score(x);
                return new RankedRow(r.DocId, score, r.Relevance, r.DocText);
            })
            .OrderByDescending(rr => rr.Score)
            .ToList();

        foreach (var rr in ranked)
            RankedRows.Add(rr);


        PrecisionAt3 = PrecisionAtK.Compute(
            rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankingRow(r.QueryId, r.QueryText, r.DocId, r.DocText, r.Relevance);
                })
                .OrderByDescending(r => model.Score(FeatureExtractor.Extract(r)))
                .ToList(),
            k: 3);

        NdcgAt3 = Ndcg.Compute(
            rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankingRow(r.QueryId, r.QueryText, r.DocId, r.DocText, r.Relevance);
                })
                .OrderByDescending(r => model.Score(FeatureExtractor.Extract(r)))
                .ToList(),
            k: 3);
        }

        [RelayCommand]

        private void TrainSelected()
        {
            RankedRows.Clear();

            if (_allRows.Count == 0 || string.IsNullOrWhiteSpace(SelectedQuery))
                return;

            var queryId = SelectedQuery.Split('—')[0].Trim();
            var rows = _allRows.Where(r => r.QueryId == queryId).ToList();
            if (rows.Count < 2) return;

            LinearScoringModel model = SelectedAlgorithm switch
            {
                RankingAlgorithm.Pointwise => PointwiseTrainer.Train(rows, epochs: 50, learningRate: 0.01f),
                RankingAlgorithm.Pairwise => PairwiseTrainer.Train(rows, epochs: 50, learningRate: 0.01f),
                RankingAlgorithm.Listwise => ListwiseTrainer.Train(rows, epochs: 80, learningRate: 0.02f),
                _ => throw new NotSupportedException()
            };

            var ranked = rows
                .Select(r =>
                {
                    var x = FeatureExtractor.Extract(r);
                    var score = model.Score(x);
                    return new RankedRow(r.DocId, score, r.Relevance, r.DocText);
                })
                .OrderByDescending(rr => rr.Score)
                .ToList();

            foreach (var rr in ranked)
                RankedRows.Add(rr);


            var rankedAsRows = ranked.Select(rr =>
                new RankingDemo.Core.Data.RankingRow(queryId, SelectedQuery!, rr.DocId, rr.DocText, rr.Relevance)
            ).ToList();

            PrecisionAtKValue = RankingDemo.Core.Evaluation.PrecisionAtK.Compute(rankedAsRows, k: K);
            NdcgAtKValue = RankingDemo.Core.Evaluation.Ndcg.Compute(rankedAsRows, k: K);
        }
        #endregion
    

    partial void OnSelectedQueryChanged(string? value)
    {
        RefreshRows();
    }

    private void RefreshRows()
    {
        RowsForSelectedQuery.Clear();

        if (string.IsNullOrWhiteSpace(SelectedQuery)) return;

        var queryId = SelectedQuery.Split('—')[0].Trim();

        foreach (var row in _allRows.Where(r => r.QueryId == queryId))
            RowsForSelectedQuery.Add(row);
    }
}
