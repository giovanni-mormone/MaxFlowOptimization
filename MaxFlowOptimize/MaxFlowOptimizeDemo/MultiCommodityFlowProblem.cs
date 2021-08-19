using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    class MultiCommodityFlowProblem : AbstractFlowProblem
    {
        protected override void InitializeObjectiveCoeffs(JsonProblem loadedProblem)
        {
            List<double> obj = RepeatedZeroList(loadedProblem.Edges.Count)
                           .Select((x, y) => edges.Select((xx, yy) => loadedProblem.Sources.Select(x => x.Name).Contains(xx.Source) ? yy : -1).Contains(y) ? 1.0 : 0.0).ToList();
            objectiveCoeffs = RangeList(loadedProblem.Commodities.Count).SelectMany(_ => obj.ToList()).ToList();
        }

        protected override void InitializeRows(JsonProblem loadedProblem)
        {
            rows = nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                //se sono una sorgente vuol dire che esce il flusso da me -> -1; se sono destinazione entra -> 1
                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = edge.edge.Source == node ? -1 : 1);
                return RangeList(loadedProblem.Commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(), 0, 'E'));
            }).ToHashSet();

            rows = rows.Concat(sources.SelectMany(source => {
                var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Source == source.Name).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
                var myCommodities = loadedProblem.CommoditiesSources.Where(x => x.Source == source.Name).Select(xx => xx.Commodity);
                var contained = commodities.Where(commo => myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
                double weight = source.Capacity == -1 ? INFINITY : source.Capacity;
                return RangeList(loadedProblem.Commodities.Count).
                    Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(),
                     contained.Contains(x) ? weight : 0, 'L'));
            })).ToHashSet();
            rows = rows.Concat(sinks.SelectMany(sink => {
                var sinkEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Destination == sink.Name).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                sinkEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
                var myCommodities = loadedProblem.CommoditiesSinks.Where(x => x.Sink == sink.Name).Select(xx => xx.Commodity);
                var contained = commodities.Where(commo => myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
                double weight = sink.Capacity == -1 ? INFINITY : sink.Capacity;
                return RangeList(loadedProblem.Commodities.Count).
                    Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(),
                     contained.Contains(x) ? weight : 0, 'L'));
            })).ToHashSet();
            rows = rows.Concat(nodes.SelectMany(node =>
                edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node && !sinks.Select(x => x.Name).Contains(combo.edge.Destination))
                    .Select(edge =>
                        new Row(RangeList(loadedProblem.Commodities.Count).SelectMany(_ => RepeatedZeroList(loadedProblem.Edges.Count)
                            .Select((value, column) => column == edge.column ? 1 : value).ToList()).ToArray(), edge.edge.Weigth, 'L')
                )
            )).ToHashSet();
        }
    }
}
