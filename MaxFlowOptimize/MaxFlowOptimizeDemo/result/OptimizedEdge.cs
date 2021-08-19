using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo.result
{
    /// <summary>
    /// A record used to represent the optimal value of an edge.
    /// </summary>
    /// <param name="Source">The <see cref="string"/> representing the source of the edge.</param>
    /// <param name="Destination">The <see cref="string"/> representing the destination of the edge.</param>
    /// <param name="Commodity">The <see cref="string"/> representing the commodity of the edge.</param>
    /// <param name="Value">The <see cref="double"/> represeting the optimal value of the edge.</param>

    public record OptimizedEdge(string Source, string Destination, string Commodity, double Value)
    {
        override public string ToString() => $"The source node is {Source}, the destination node is {Destination}, the commodity is {Commodity}, the optimized value for the edge is {Value}";
    }
}
