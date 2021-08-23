using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxFlowOptimizeDemo
{
    public static class DeconstructList
    {
        public static void Deconstruct<T>(this List<T> list, out T head, out List<T> tail)
        {
            head = list.FirstOrDefault();
            tail = new List<T>(list.Skip(1));
        }
    }
    class MultiCommodityFlowProblem : AbstractFlowProblem
    {
        protected override void InitializeObjectiveCoeffs(JsonProblem loadedProblem)
        {
            List<double> obj = RepeatedZeroList(loadedProblem.Edges.Count)
                           .Select((x, y) => edges.Select((xx, yy) => loadedProblem.CommoditiesSources.Select(x => x.Name).Contains(xx.Source) ? yy : -1).Contains(y) ? 1.0 : 0.0).ToList();
            objectiveCoeffs = RangeList(loadedProblem.Commodities.Count).SelectMany(_ => obj.ToList()).ToList();
        }
        private HashSet<Row> jaja(JsonProblem loadedProblem)
        { 
            HashSet<Row> _jaja(List<CommoditySourceSink> sources, HashSet<Row> rowRecursive)=> sources switch
            {
                (CommoditySourceSink source, _) when sources.Count==1 =>
                          rowRecursive.Append(InitializeRows2(Metodino(loadedProblem, source))).Concat(InitializeRows3(Metodino(loadedProblem, source))).ToHashSet(),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name==tail.First().Name=>
                        _jaja(tail, rowRecursive.Append(InitializeRows2(Metodino(loadedProblem,source))).ToHashSet()),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name != tail.First().Name =>
                        _jaja(tail, rowRecursive.Append(InitializeRows2(Metodino(loadedProblem, source))).Concat(InitializeRows3(Metodino(loadedProblem, source))).ToHashSet()),
                _ => rowRecursive,
            };
            return _jaja(sources.OrderBy(x=>x.Name).ToList(), new());
        }
        private (JsonProblem loadedProblem, CommoditySourceSink source, List<double> rowCoeffs) Metodino(JsonProblem loadedProblem, CommoditySourceSink source)
        {
            var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Source == source.Name).ToList();
            var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
            sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
            return (loadedProblem, source, rowCoeffs);
        } 
        private Row InitializeRows2((JsonProblem loadedProblem, CommoditySourceSink source, List<double> rowCoeffs) values)
        {  
                var contained = commodities.First(commo => commo.CommodityName == values.source.Commodity).CommodityNumber;
                double weight = values.source.Capacity == -1 ? INFINITY : values.source.Capacity;
                return new Row(CreateRow(values.rowCoeffs, contained, values.loadedProblem.Commodities.Count).ToArray(), weight, 'L');
                
        }
        private  HashSet<Row> InitializeRows3((JsonProblem loadedProblem, CommoditySourceSink source, List<double> rowCoeffs) values)
        {
                var myCommodities = values.loadedProblem.CommoditiesSources.Where(x => x.Name == values.source.Name).Select(xx => xx.Commodity);
                var contained = commodities.Where(commo => !myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
                return contained.Select(x => new Row(CreateRow(values.rowCoeffs, x, values.loadedProblem.Commodities.Count).ToArray(), 0, 'L')).ToHashSet();
        }
        protected override void InitializeRows(JsonProblem loadedProblem)
        {
            rows = jaja(loadedProblem);
            
            /*sources.SelectMany(source => {
                var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Source == source.Name).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
                //var myCommodities = loadedProblem.CommoditiesSources.Where(x => x.Name == source.Name).Select(xx => xx.Commodity);
                var contained = commodities.First(commo => commo.CommodityName == source.Commodity).CommodityNumber;
                double weight = source.Capacity == -1 ? INFINITY : source.Capacity;
                HashSet<Row> row = new();
                row.Add(new Row(CreateRow(rowCoeffs, contained, loadedProblem.Commodities.Count).ToArray(), weight, 'L'));
                return row;
            }).ToHashSet();

            rows = rows.Concat(sources.Select(x => x.Name).SelectMany(source =>
            {
                var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Source == source).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
                var myCommodities = loadedProblem.CommoditiesSources.Where(x => x.Name == source).Select(xx => xx.Commodity);
                var contained = commodities.Where(commo => !myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
                return contained.Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(),0, 'L'));
            })).ToHashSet();*/

            rows = rows.Concat(sinks.SelectMany(sink => {
                var sinkEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Destination == sink.Name).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                sinkEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
                var myCommodities = loadedProblem.CommoditiesSinks.Where(x => x.Name == sink.Name).Select(xx => xx.Commodity);
                var contained = commodities.Where(commo => myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
                double weight = sink.Capacity == -1 ? INFINITY : sink.Capacity;
                return RangeList(loadedProblem.Commodities.Count).
                    Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(),
                     contained.Contains(x) ? weight : 0, 'L'));
            })).ToHashSet();

            rows = rows.Concat(nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                //se sono una sorgente vuol dire che esce il flusso da me -> -1; se sono destinazione entra -> 1
                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = edge.edge.Source == node ? -1 : 1);
                return RangeList(loadedProblem.Commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Count).ToArray(), 0, 'E'));
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
