using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxFlowOptimizeDemo
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

    /// <summary>
    /// A class used to model a flow problem, using a <see cref="JsonProblem"/> as a source to create and model a flow
    /// problem.
    /// </summary>
    public class FlowProblem
    {
        private static readonly double INFINITY = WrapperCoinMP.WrapperCoin.GetInfinity();
        private HashSet<string> nodes;
        private HashSet<SinkSource> sources;
        private HashSet<SinkSource> sinks;
        private List<double> objectiveCoeffs;
        private HashSet<Edge> edges;
        private HashSet<Row> rows;
        private HashSet<Commodity> commodities;

        
        public static FlowProblem InizializeProblem(JsonProblem loadedProblem)
        {
            FlowProblem problem = new();
            problem.InitializeNodesAndEdges(loadedProblem);
            problem.InitializeObjectiveCoeffs(loadedProblem);
            problem.InitializeRows(loadedProblem);
            return problem;

        }
        /// <summary>
        /// Method used to create a <see cref="Result"/> from the given parameters
        /// </summary>
        /// <param name="result"> The <see cref="double"/> result.</param>
        /// <param name="optimizedValues"> The <see cref="List{double}"/> optimized values for each edge.</param>
        /// <returns></returns>
        public Result CreateResult(double result, List<double> optimizedValues)
        {
            var x = commodities.SelectMany(commodity => edges.Select((edge,edgeIndex) => 
                new OptimizedEdge(edge.Source,edge.Destination, commodity.CommodityName, optimizedValues.ElementAt(ComputeIndexInEdgeResult(edgeIndex,commodity.CommodityNumber, edges.Count)))));
            return new Result(result, x.ToList());
        }

        /// <summary>
        /// Method used to get the <see cref="HashSet{T}"/> of <see cref="Row"/> representing the rows of the problem
        /// loaded and parsed from the <<see cref="JsonProblem"/>.
        /// </summary>
        /// <returns>The <see cref="HashSet{T}"/> of <see cref="Row"/> of the problem.</returns>
        public HashSet<Row> GetRows() => rows;
        /// <summary>
        /// Method used to get the coeffs of the objective function of the flow problem to optimize.
        /// </summary>
        /// <returns>An <see cref="Array"/> of <see cref="double"/> representing the coeffs of the objective function.</returns>
        public double[] GetObjectiveCoeffs() => objectiveCoeffs.ToArray();

        private FlowProblem() { }

        private static readonly Func<int, List<double>> RepeatedZeroList = length => Enumerable.Repeat(0.0, length).ToList();
        private static readonly Func<int, HashSet<int>> RangeList = length => Enumerable.Range(0, length).ToHashSet();
        private static readonly Func<int, int, int, int> ComputeIndexInEdgeResult = (edgeIndex, commodity, totalEdges ) => edgeIndex + totalEdges * commodity;

        private void InitializeNodesAndEdges(JsonProblem loaded) {
            commodities = loaded.Commodities.Select((name, id) => (name, id)).Select(x => new Commodity(x.name, x.id)).ToHashSet();
            sources = loaded.Sources.ToHashSet();
            sinks = loaded.Sinks.ToHashSet();
            nodes = loaded.Nodes.ToHashSet();
            edges = RangeList(loaded.Edges.Count).Select(x => loaded.Edges.ElementAt(x)).ToHashSet();
        }
        private void InitializeObjectiveCoeffs(JsonProblem loadedProblem) {
            List<double> obj = RepeatedZeroList(loadedProblem.Edges.Count)
                .Select((x,y) => edges.Select((xx, yy) => loadedProblem.Sources.Select(x => x.Name).Contains(xx.Source) ? yy : -1).Contains(y)?1.0:0.0).ToList();
            objectiveCoeffs = RangeList(loadedProblem.Commodities.Count).SelectMany(_ => obj.ToList()).ToList();
        }
        private void InitializeRows(JsonProblem loadedProblem)
        {
            rows = nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge,column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
                //se sono una sorgente vuol dire che esce il flusso da me -> -1; se sono destinazione entra -> 1
                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = edge.edge.Source == node ? -1 : 1);
                return RangeList(loadedProblem.Commodities.Count).Select(x => new Row(CreateRow(rowCoeffs,x, loadedProblem.Commodities.Count).ToArray(),0,'E'));
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
