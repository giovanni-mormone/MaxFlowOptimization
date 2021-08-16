using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// Record used to represent a pair of a Source and a Commodity, read from the json of the problem.
    /// </summary>
    ///<param name="Source">The name of the source.</param>
    ///<param name="Commodity">The name of the commodity.</param>

    public record CommoditySource(string Source, string Commodity);
}
