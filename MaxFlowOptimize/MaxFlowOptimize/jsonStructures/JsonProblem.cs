using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using System.Collections.Generic;

namespace MaxFlowOptimizeDemo.jsonStructures
{
    /// <summary>
    /// Record used to read and deserialize the data from a json file.
    /// </summary>
    ///<param name="Nodes">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="System.String"/> representing the nodes of the problem.</param>
    ///<param name="Commodities">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="System.String"/> representing the commodities of the problem.</param>
    ///<param name="CommoditiesSources">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.CommoditySourceSink"/> of the problem.</param>
    ///<param name="CommoditiesSinks">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.CommoditySourceSink"/> of the problem.</param>
    ///<param name="Edges">The <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="MaxFlowOptimizeDemo.Edge"/> of the problem.</param>
    public record JsonProblem(HashSet<string> Nodes, HashSet<string> Commodities, HashSet<CommoditySourceSink> CommoditiesSources, HashSet<CommoditySourceSink> CommoditiesSinks, HashSet<Edge> Edges);
}
