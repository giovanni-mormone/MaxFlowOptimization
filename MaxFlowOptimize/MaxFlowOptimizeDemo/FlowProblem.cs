using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    public record Result(double Objective, List<OptimizedEdges> EdgesResult) 
    {
        public override string ToString() => $"The optimized total flow value is {Objective}, and the edges optimized values are\n {PrintOptimizedList}";

        private string PrintOptimizedList => string.Join("\n", EdgesResult.Select(x => $"{x}").ToArray());
    }

    public record OptimizedEdges(string Source, string Destination, string Commodity, double Value)
    {
        override public string ToString() => $"The source node is {Source}, the destination node is {Destination}, the commodity is {Commodity}, the optimized value for the edge is {Value}";
    }

    public class FlowProblem
    {
        private IList<string> nodes;
        private IList<string> sources;
        private IList<string> sinks;
        private IList<double> objectiveCoeffs;
        private IList<Edge> edges;
        private IList<Row> rows;
        private IList<Commodity> commodities;

        public static FlowProblem InizializeProblem(JsonProblem loadedProblem)
        {
            FlowProblem problem = new();
            problem.InitializeNodesAndEdges(loadedProblem);
            problem.InitializeObjectiveCoeffs(loadedProblem);
            problem.InitializeRows(loadedProblem);
            return problem;

        }

        public Result CreateResult(double result, List<double> optimizedValues)
        {
            var x = commodities.SelectMany(commodity => edges.Select((edge,edgeIndex) => 
                new OptimizedEdges(edge.Source,edge.Destination, commodity.CommodityName, optimizedValues.ElementAt(ComputeIndexInEdgeResult(edgeIndex,commodity.CommodityNumber, edges.Count)))));
            return new Result(result, x.ToList());
        }
        public IList<Row> GetRows() => rows;
        public double[] GetObjectiveCoeffs() => objectiveCoeffs.ToArray();

        private FlowProblem() { }

        private static readonly Func<int, List<double>> RepeatedZeroList = length => Enumerable.Repeat(0.0, length).ToList();
        private static readonly Func<int, List<int>> RangeList = length => Enumerable.Range(0, length).ToList();
        private static readonly Func<int, int, int, int> ComputeIndexInEdgeResult = (edgeIndex, commodity, totalEdges ) => edgeIndex + totalEdges * commodity;

        private void InitializeNodesAndEdges(JsonProblem loaded) {
            commodities = loaded.Commodities.Names.Select((name, id) => (name, id)).Select(x => new Commodity(x.name, x.id)).ToList();
            sources = loaded.Sources.Names.ToList();
            sinks = loaded.Sinks.Names.ToList();
            nodes = loaded.Nodes.Names.ToList();
            edges = RangeList(loaded.Edges.EdgesNumber).Select(x => loaded.Edges.EdgesDirection.ElementAt(x)).ToList();
        }
        private void InitializeObjectiveCoeffs(JsonProblem loadedProblem) {
            List<double> obj = RepeatedZeroList(loadedProblem.Edges.EdgesNumber)
                .Select((x,y) => edges.Select((xx, yy) => loadedProblem.Sources.Names.Contains(xx.Source) ? yy : -1).Contains(y)?1.0:0.0).ToList();
            objectiveCoeffs = RangeList(loadedProblem.Commodities.Number).SelectMany(_ => obj.ToList()).ToList();
        }
        private void InitializeRows(JsonProblem loadedProblem)
        {
            rows = nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge,column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.EdgesNumber);
                //se sono una sorgente vuol dire che esce il flusso da me -> -1; se sono destinazione entra -> 1
                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = edge.edge.Source == node ? -1 : 1);
                return RangeList(loadedProblem.Commodities.Number).Select(x => new Row(CreateRow(rowCoeffs,x, loadedProblem.Commodities.Number).ToArray(),0,'E'));
            }).ToList();

            rows = rows.Concat(sources.SelectMany(source => {
                var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(combo => combo.edge.Source == source).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.EdgesNumber);
                sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
                var myCommodities = loadedProblem.CommoditiesSources.Sources.Where(x => x.Source == source).Select(xx => xx.Commodity);
                var contained = commodities.Where(commo => myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
                double weight = sourceEdges.First(x => x.edge.Source == source).edge.Weigth;
                return RangeList(loadedProblem.Commodities.Number).
                    Select(x => new Row(CreateRow(rowCoeffs, x, loadedProblem.Commodities.Number).ToArray(), 
                     contained.Contains(x)?weight:0, 'L'));
            })).ToList();
            rows = rows.Concat(nodes.SelectMany(node => 
                edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node && !sinks.Contains(combo.edge.Destination))
                    .Select(edge => 
                        new Row(RangeList(loadedProblem.Commodities.Number).SelectMany(_ => RepeatedZeroList(loadedProblem.Edges.EdgesNumber)
                            .Select((value, column) => column == edge.column ? 1 : value).ToList()).ToArray(), edge.edge.Weigth, 'L')
                )
            )).ToList();
        }

        private static List<double> CreateRow(List<double> coeffs, int commodity, int totalCommodities)
        {
            var zeroRow = RepeatedZeroList(coeffs.Count);
            List<double> _createRow(IEnumerable<double> origin, int step) => step switch
            {
                _ when step == totalCommodities => origin.ToList(),
                _ when step == commodity => _createRow(origin.Concat(coeffs), step + 1),
                _ => _createRow(origin.Concat(zeroRow), step + 1)
            };
            return _createRow(Enumerable.Empty<double>(), 0);
        }
    }
}
