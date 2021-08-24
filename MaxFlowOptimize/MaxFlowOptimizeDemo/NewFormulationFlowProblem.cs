using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// Implementation to Deal with the new formulation of the problem; it compacts the restraints for the capacity of sinks and rows.
    /// </summary>
    class NewFormulationFlowProblem : AbstractFlowProblem
    {
        private readonly int nMaxMultiplier;

        public NewFormulationFlowProblem(int nMax) : base()
        {
            nMaxMultiplier = nMax;
        }
        protected override void InitializeObjectiveCoeffs(JsonProblem loadedProblem)
        {
            //the first method creates an objective function with the aim to maximize the input received by the sink nodes;
            //in this case it tries to maximize all the edges that enters a sink. It sets at 1 the variable that represents an edge
            //with a sink as destination.
            List<double> obj = RepeatedZeroList(loadedProblem.Edges.Count)
                                       .Select((x, y) => edges.Select((xx, yy) => loadedProblem.CommoditiesSinks.Any(x => x.Name == xx.Destination) ? yy : -1).Contains(y) ? 1.0 : 0.0).ToList();
            objectiveCoeffs = RangeList(loadedProblem.Commodities.Count).SelectMany(_ => obj.ToList()).ToList();
        }

        protected override void InitializeRows(JsonProblem loadedProblem)
        {

            base.InitializeRows(loadedProblem);
            SetContinuityConstraints(loadedProblem, FirstContinuityRestraint);
            SetContinuityConstraints(loadedProblem,SecondContinuityRestraint);
        }
        private void SetContinuityConstraints(JsonProblem loadedProblem, Func<bool,int> SetCoeffs)
        {
            rows = rows.Concat(nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);

                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = SetCoeffs(edge.edge.Destination == node));
                return RangeList(loadedProblem.Commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(), 0, 'L'));
            })).ToHashSet();
        }
        private int FirstContinuityRestraint(bool y) => y ? 1 : -1;
        private int SecondContinuityRestraint(bool y) => y ? -1 * nMaxMultiplier : 1;

    }
}
