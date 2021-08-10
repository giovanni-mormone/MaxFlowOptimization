using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    public record JsonProblem(Pair Sources, Pair Sinks, Pair Nodes, Pair Commodities, CommoditiesSources CommoditiesSources, Edges Edges);
}
