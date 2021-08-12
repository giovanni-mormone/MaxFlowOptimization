using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    public record JsonProblem(HashSet<SinkSource> Sources, HashSet<SinkSource> Sinks, HashSet<string> Nodes, HashSet<string> Commodities, HashSet<CommoditySource> CommoditiesSources, HashSet<CommoditySink> CommoditiesSinks, HashSet<Edge> Edges);
}
