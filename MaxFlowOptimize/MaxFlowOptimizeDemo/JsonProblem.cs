using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// Record used to read and deserialize the data from a json file.
    /// </summary>
    ///<param name="Sources">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.SinkSource"/> representing the source nodes for the problem.</param>
    ///<param name="Sinks">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.SinkSource"/> representing the sink nodes for the problem.</param>
    ///<param name="Nodes">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="System.String"/> representing the nodes of the problem.</param>
    ///<param name="Commodities">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="System.String"/> representing the commodities of the problem.</param>
    ///<param name="CommoditiesSources">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.CommoditySource"/> of the problem.</param>
    ///<param name="CommoditiesSinks">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.CommoditySink"/> of the problem.</param>
    ///<param name="Edges">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.Edge"/> of the problem.</param>
    public record JsonProblem(HashSet<SinkSource> Sources, HashSet<SinkSource> Sinks, HashSet<string> Nodes, HashSet<string> Commodities, HashSet<CommoditySource> CommoditiesSources, HashSet<CommoditySink> CommoditiesSinks, HashSet<Edge> Edges);
}
