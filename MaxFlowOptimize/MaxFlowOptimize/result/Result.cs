using System.Collections.Generic;
using System.Linq;

namespace MaxFlowOptimizeDemo.result
{
    /// <summary>
    /// Record that represents the result of an optimized problem.
    /// </summary>
    /// <param name="Objective">The <see cref="double"/> value representing the optimal value for the flow</param>
    /// <param name="EdgesResult"> The <see cref="List{OptimizedEdge}"/> representing the optimal value of each edge.</param>
    public record Result(double Objective, List<OptimizedEdge> EdgesResult)
    {
        public override string ToString() => $"The optimized total flow value is {Objective}, and the edges optimized values are\n {PrintOptimizedList}";

        private string PrintOptimizedList => string.Join("\n", EdgesResult.Select(x => $"{x}").ToArray());
    }
}
