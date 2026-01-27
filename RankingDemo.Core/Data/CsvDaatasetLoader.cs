using System.Globalization;
using System.Text;

namespace RankingDemo.Core.Data;

public static class CsvDatasetLoader
{
    public static List<RankingRow> Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("A megadott CSV fájl nem található.", path);

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return new List<RankingRow>();

        var rows = new List<RankingRow>(Math.Max(0, lines.Length - 1));

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = SplitCsvLine(line);
            if (parts.Count < 5) continue;

            var queryId = parts[0].Trim();
            var queryText = parts[1].Trim();
            var docId = parts[2].Trim();
            var docText = parts[3].Trim();

            if (!int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rel))
                rel = 0;

            rows.Add(new RankingRow(queryId, queryText, docId, docText, rel));
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
