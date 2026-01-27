using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingDemo.Core.Data
{
    public sealed record RankingRow(
    string QueryId,
    string QueryText,
    string DocId,
    string DocText,
    int Relevance
        );
}
