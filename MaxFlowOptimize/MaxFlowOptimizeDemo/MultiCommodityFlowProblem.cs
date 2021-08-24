using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using MaxFlowOptimizeDemo.Utils;

namespace MaxFlowOptimizeDemo
{
    class MultiCommodityFlowProblem : AbstractFlowProblem
    {
        protected override void InitializeObjectiveCoeffs(JsonProblem loadedProblem)
        {
            List<double> obj = RepeatedZeroList(loadedProblem.Edges.Count)
                           .Select((x, y) => edges.Select((xx, yy) => loadedProblem.CommoditiesSources.Select(x => x.Name).Contains(xx.Source) ? yy : -1).Contains(y) ? 1.0 : 0.0).ToList();
            objectiveCoeffs = RangeList(loadedProblem.Commodities.Count).SelectMany(_ => obj.ToList()).ToList();
        }
        

        
        protected override void InitializeRows(JsonProblem loadedProblem)
        {
            base.InitializeRows(loadedProblem);
            rows = rows.Concat(nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                //se sono una sorgente vuol dire che esce il flusso da me -> -1; se sono destinazione entra -> 1
                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = edge.edge.Source == node ? -1 : 1);
                return RangeList(loadedProblem.Commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(), 0, 'E'));
            })).ToHashSet();

            


        }
    }
}
