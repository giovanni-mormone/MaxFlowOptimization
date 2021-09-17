using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using System;
using System.Collections.Generic;
using System.Linq;
using MaxFlowOptimizeDemo.Utils;

namespace MaxFlowOptimizeDemo
{

    /// <summary>
    /// A class used to model a flow problem, using a <see cref="JsonProblem"/> as a source to create and model a flow
    /// problem.
    /// This alternative implementation sets sources and sink that don't have some commodity to 0
    /// </summary>
    public class FlowProblemFormulationAlt : IFlowProblem
    {
        private static readonly double INFINITY = WrapperCoinMP.WrapperCoin.GetInfinity();
        private HashSet<string> nodes;
        private HashSet<CommoditySourceSink> sources;
        private HashSet<CommoditySourceSink> sinks;
        private List<double> objectiveCoeffs;
        private HashSet<Edge> edges;
        private HashSet<Row> rows;
        private HashSet<Commodity> commodities;
        private readonly int nMaxMultiplier;
        private HashSet<Edge> penality = new();
        Dictionary<string, List<string>> commodityGroups = new();

        public FlowProblemFormulationAlt(int nMax)
        {
            nMaxMultiplier = nMax;
        }

        /// <summary>
        /// Method used to initialize a <see cref="IFlowProblem"/>.
        /// </summary>
        /// <param name="loadedProblem">The <see cref="JsonProblem"/> to initialize.</param>
        public void InizializeProblem(JsonProblem loadedProblem, Dictionary<string, List<string>> commoidityGroups)
        {
            this.commodityGroups = commoidityGroups;
            InitializeNodesAndEdges(loadedProblem);
            //InitializeObjectiveCoeffs();
            InitializeObjectiveCoeffsNewVersion();
            InitializeRows();
            InitializeCapacityRows();
        }

        public void CreateLagrangian(JsonProblem loadedProblem, Dictionary<String, List<String>> commodityGroups)
        {
            this.commodityGroups = commodityGroups;
            InitializeNodesAndEdges(loadedProblem);
            InitializeObjectiveCoeffsLagrangian();
            InitializeRows();

        }


        public void InizializeProblemAlternativeFormulation(JsonProblem loadedProblem)
        {
            InitializeNodesAndEdges(loadedProblem);
            InitializeObjectiveCoeffsAlt();
            InitializeRowsAlternative();
        }
        /// <summary>
        /// Method used to create a <see cref="Result"/> from the given parameters
        /// </summary>
        /// <param name="result"> The <see cref="double"/> result.</param>
        /// <param name="optimizedValues"> The <see cref="List{double}"/> optimized values for each edge.</param>
        /// <returns></returns>
        public Result CreateResult(double result, List<double> optimizedValues)
        {
            var x = commodities.SelectMany(commodity => edges.Select((edge, edgeIndex) =>
                new OptimizedEdge(edge.Source, edge.Destination, commodity.CommodityName, optimizedValues.ElementAt(ComputeIndexInEdgeResult(edgeIndex, commodity.CommodityNumber, edges.Count)))));
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


        public void AddPenality(Edge edge) => penality.Add(edge);
        private List<double> CreateRow(List<double> coeffs, int commodity, int totalCommodities)
        {
            var zeroRow = RepeatedZeroList(coeffs.Count);
            var finalZero = RepeatedZeroList(coeffs.Count * commodityGroups.Keys.Count);
            List<double> _createRow(IEnumerable<double> origin, int step) => step switch
            {
                _ when step == totalCommodities => origin.Concat(finalZero).ToList(),
                _ when step == commodity => _createRow(origin.Concat(coeffs), step + 1),
                _ => _createRow(origin.Concat(zeroRow), step + 1)
            };
            return _createRow(Enumerable.Empty<double>(), 0);
        }

        private void InitializeNodesAndEdges(JsonProblem loaded)
        {
            commodities = loaded.Commodities.Select((name, id) => (name, id)).Select(x => new Commodity(x.name, x.id)).ToHashSet();
            sources = loaded.CommoditiesSources.ToHashSet();
            sinks = loaded.CommoditiesSinks.ToHashSet();
            nodes = loaded.Nodes.ToHashSet();
            edges = RangeList(loaded.Edges.Count).Select(x => loaded.Edges.ElementAt(x)).ToHashSet();
        }

        private void InitializeObjectiveCoeffs()
        {
            List<double> test = RepeatedZeroList(edges.Count * commodities.Count).ToList();
            edges.Select((edge, index) => (edge, index)).ToList().ForEach(couple =>
            {
                sinks.Where(sink => sink.Name == couple.edge.Destination).ToList().ForEach(sink2 =>
                {
                    commodities.Where(co => co.CommodityName == sink2.Commodity).ToList().ForEach(commo =>
                    {
                        test[couple.index + commo.CommodityNumber * edges.Count] = 1;
                    });
                });
            });
            objectiveCoeffs = test.ToList();

        }

        private void InitializeObjectiveCoeffsNewVersion()
        {
            List<double> test = RepeatedZeroList(edges.Count * (commodities.Count + commodityGroups.Keys.Count)).ToList();
            edges.Select((edge, index) => (edge, index)).ToList().ForEach(couple =>
            {
                penality.Where(penal => penal.Destination == couple.edge.Destination && penal.Source == couple.edge.Source).ToList().ForEach(sink2 =>
                {
                    commodities.ToList().ForEach(commo =>
                    {
                        test[couple.index + commo.CommodityNumber * edges.Count] = 1000;
                    });
                });
            });
            objectiveCoeffs = test.ToList();

        }

        private void InitializeObjectiveCoeffsLagrangian()
        {
            List<double> test = RepeatedZeroList(edges.Count * (commodities.Count)).ToList();
            List<double> testLagPenalty = RepeatedZeroList(edges.Count * commodityGroups.Keys.Count).Select(x => x + new Random().Next(1, 30)).ToList();
            edges.Select((edge, index) => (edge, index)).ToList().ForEach(couple =>
            {
                penality.Where(penal => penal.Destination == couple.edge.Destination && penal.Source == couple.edge.Source).ToList().ForEach(sink2 =>
                {
                    commodities.ToList().ForEach(commo =>
                    {
                        test[couple.index + commo.CommodityNumber * edges.Count] = 1000;
                    });
                });
            });
            objectiveCoeffs = test.Concat(testLagPenalty).ToList();

        }
        private void InitializeObjectiveCoeffsAlt()
        {

            List<double> test = Enumerable.Repeat(1.0, edges.Count * commodities.Count).ToList();
            edges.Select((edge, index) => (edge, index)).ToList().ForEach(couple =>
            {
                penality.Where(penal => penal.Destination == couple.edge.Destination && penal.Source == couple.edge.Source).ToList().ForEach(sink2 =>
                {
                    commodities.ToList().ForEach(commo =>
                    {
                        test[couple.index + commo.CommodityNumber * edges.Count] = 1000;
                    });
                });
            });
            //List<double> obj = RepeatedZeroList(edges.Count)
            //                       .Select((x, y) => edges.Select((xx, yy) => sinks.Any(x => x.Name == xx.Destination) ? yy : -1).Contains(y) ? 1.0 : 0.0).ToList();

            objectiveCoeffs = test.Concat(RepeatedZeroList(edges.Count * commodities.Count)).ToList();//RangeList(commodities.Count).SelectMany(_ => obj.ToList()).ToList();//

        }

        private void InitializeRows()
        {
            rows = SourceSinkInitialize(true);
            rows = rows.Concat(SourceSinkInitialize(false)).ToHashSet();


            rows = rows.Concat(nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(edges.Count);
                //se sono una sorgente vuol dire che esce il flusso da me -> -1; se sono destinazione entra -> 1
                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = edge.edge.Source == node ? -1 : 1);
                return RangeList(commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, commodities.Count).ToArray(), 0, 'E'));
            })).ToHashSet();

            commodityGroups.Select(pair => pair.Value).ToList().ForEach(commodities =>
            {
                rows = rows.Concat(edges.Where(edge => sinks.All(sink => sink.Name != edge.Destination)).Select((edge,column)=>
                    new Row(constructMultiCommodityRow(commodities, edge, column), 0, 'L'))).ToHashSet();
            });
            /*
            x = commoidityGroups.Select(pair =>
               edges.Where(edge => sinks.All(sink => sink.Name != edge.Destination)).Select((edge, column) =>
                   new Row(constructMultiCommodityRow(pair.Value), nMaxMultiplier, 'L')));
  */

            /*var med = edges.Where(edge => sinks.All(sink => sink.Name != edge.Destination)).Select((edge,column) =>
                new Row(constructMultiCommodityRow(), nMaxMultiplier, 'L'))).ToHashSet();
            /*/
        }

        private void InitializeCapacityRows()
        {
            rows = rows.Concat(edges.Select((edge, column) =>
               new Row(RangeList(commodities.Count + commodityGroups.Keys.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count)
                   .Select((value, col) => col == column && commodityNumber >= commodities.Count ? FindAkCommodity(findChildCommodityNumber(commodityNumber)) : value).ToList()).ToArray(), edge.Weigth == -1 ? INFINITY : edge.Weigth, 'L')))
               .ToHashSet();
        }
        private int findChildCommodityNumber(int num)
        {
            num = num - commodities.Count();
            var key = commodityGroups.Keys.ToList().ElementAt(num);
            var ret = commodities.First(x => x.CommodityName == commodityGroups[key].First()).CommodityNumber;
            return ret;
        }

        private double[] constructMultiCommodityRow(List<string> commo, Edge edge, int column)
        {

            var arra = RangeList(commodities.Count + commodityGroups.Keys.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count).
                Select((value, col) => col == column && commodities.Where(x => commo.Contains(x.CommodityName)).Any(x => x.CommodityNumber == commodityNumber) ? 1.0 : 
                col == column && commodityNumber >= commodities.Count && commodities.Where(x => commo.Contains(x.CommodityName)).Any(x => x.CommodityNumber == findChildCommodityNumber(commodityNumber)) ? -nMaxMultiplier : 0).ToList()).ToArray();
            
            return arra;
        }

        private HashSet<Row> EdgeConstraints(Edge edge, int column)
        {

            return commodities.Select(commodity =>
            {
                var predecessors = edges.Select((edge, index) => (edge, index)).Where(x => x.edge.Destination == edge.Source).ToList();

                predecessors = predecessors.Where(x => (sources.All(source => source.Name != x.edge.Source) && sinks.All(sink => sink.Name != x.edge.Source)) ||
                    sources.Any(source => source.Name == x.edge.Source && source.Commodity == commodity.CommodityName)
                    ).ToList();
                /*predecessors = predecessors.Where(x => (sources.All(sources => sources.Name != x.edge.Source) ||
                    sources.Any(source => source.Commodity == commodity.CommodityName && source.Name == x.edge.Source)) && (
                    sinks.All(sink => sink.Name != x.edge.Source) || sinks.All(sink => sink.Name != x.edge.Source || sink.Commodity != commodity.CommodityName))).ToList();
                */
                var edgerow = RepeatedZeroList(edges.Count).Select((x, col) => predecessors.Any(x => x.index == col) ? -1 : x).ToList();

                if (sinks.Any(x => x.Name == edge.Source && x.Commodity == commodity.CommodityName))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), -1, 'L');
                }
                else if (sinks.Any(x => x.Name == edge.Destination && x.Commodity != commodity.CommodityName))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), 0, 'L');
                }
                else if (sources.Any(source => source.Name == edge.Destination && source.Commodity == commodity.CommodityName))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), 0, 'L');
                }
                else if (sources.All(x => x.Name != edge.Source) && sinks.All(x => x.Name != edge.Source))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), -1, 'L');
                }
                else
                {
                    return new Row(CreateRow(RepeatedZeroList(edges.Count), commodity.CommodityNumber, commodities.Count).ToArray(), 0, 'L');
                }
            }).ToHashSet();
        }

        private HashSet<Row> SetInequalitiesConstraints()
        {

            var edgesAndColumns = edges.Select((edge, column) => (edge, column)).ToList();
            HashSet<Row> result = new HashSet<Row>();

            result = result.Concat(edgesAndColumns.SelectMany(edge =>
            {
                return EdgeConstraints(edge.edge, edge.column);
            })).ToHashSet();

            return result;
        }

        private void InitializeRowsAlternative()
        {
            rows = SetInequalitiesConstraints();
            rows = rows.Concat(edges.Select((edge, column) =>
                new Row(RangeList(commodities.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count)
                    .Select((value, col) => col == column ? FindAkCommodity(commodityNumber) : value).ToList()).Concat(RepeatedZeroList(edges.Count * commodities.Count)).ToArray(), edge.Weigth == -1 ? INFINITY : edge.Weigth, 'L')))
                .ToHashSet();
        }

        private double FindAkCommodity(int commodityNumber) => sources.First(x => x.Commodity == commodities.First(xx => xx.CommodityNumber == commodityNumber).CommodityName).Capacity;

        private void SetContinuityConstraints(Func<bool, int> SetCoeffs)
        {
            rows = rows.Concat(nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();
                var rowCoeffs = RepeatedZeroList(edges.Count);

                nodeEdges.ForEach(edge => rowCoeffs[edge.column] = SetCoeffs(edge.edge.Destination == node));
                return RangeList(commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, commodities.Count).ToArray(), 0, 'L'));
            })).ToHashSet();
        }
        private int FirstContinuityRestraint(bool y) => y ? 1 : -1;
        private int SecondContinuityRestraint(bool y) => y ? -1 * nMaxMultiplier : 1;

        //Method used to compute, in a tail recursive way, the row constraints of sources and sinks;
        //It uses the loaded problem, a boolean to decide if it is source or sink,
        private HashSet<Row> SourceSinkInitialize(bool IsSource)
        {
            //first i set the right filter functions for the tail method.
            var findSourceSink = IsSource ? FindSources : FindSinks;
            var findCommoditySourceSink = IsSource ? FindCommoditySource : FindCommoditySink;

            //then recursively compute the set of constraint for all the sources or sinks of the problem;
            //First case is the base case => when the input list has only one element left, it first computes the constraint for the CommoditySourceSink
            //then it computes all the constraints for the not contained commodities.
            //Second case is when a source/sink has multiple commodities(the input list is ordered when the function is called first time): it computes the
            //constraint for the given source/sink then it call itself recursively.
            //third case is when the element to compute is the last for the given source/sink: it computes the his constraint, then it computes the constraints for
            //all the commodities that are not generated/required by him and then it calls itself recursively.
            HashSet<Row> _SourceSinkInitialize(List<CommoditySourceSink> sources, HashSet<Row> rowRecursive) => sources switch
            {
                (CommoditySourceSink source, _) when sources.Count == 1 =>
                         rowRecursive.Append(ContainedCommodityConstraint(Metodino(source, findSourceSink)))
                         .Concat(NotContainedCommoditiesConstraints(Metodino(source, findSourceSink), findCommoditySourceSink, IsSource)).ToHashSet(),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name == tail.First().Name =>
                        _SourceSinkInitialize(tail, rowRecursive.Append(ContainedCommodityConstraint(Metodino(source, findSourceSink))).ToHashSet()),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name != tail.First().Name =>
                        _SourceSinkInitialize(tail, rowRecursive.Append(ContainedCommodityConstraint(Metodino(source, findSourceSink)))
                        .Concat(NotContainedCommoditiesConstraints(Metodino(source, findSourceSink), findCommoditySourceSink, IsSource)).ToHashSet()),
                _ => rowRecursive,
            };
            return _SourceSinkInitialize(IsSource ? sources.OrderBy(x => x.Name).ToList() : sinks.OrderBy(x => x.Name).ToList(), new());
        }

        //method used to create the SourceSinkCoeffs for the given CommoditySourceSink.
        private SourceSinkCoeffs Metodino(CommoditySourceSink source, Func<Edge, string, bool> findSourceSink)
        {
            var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(edge => findSourceSink(edge.edge, source.Name)).ToList();
            var rowCoeffs = RepeatedZeroList(edges.Count);
            sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
            return new SourceSinkCoeffs(source, rowCoeffs);
        }
        //Method that create the constraint for a given source or sink.
        private Row ContainedCommodityConstraint(SourceSinkCoeffs values)
        {
            var contained = commodities.First(commo => commo.CommodityName == values.source.Commodity).CommodityNumber;
            //double weight = values.source.Capacity == -1 ? INFINITY : values.source.Capacity;
            return new Row(CreateRow(values.rowCoeffs, contained, commodities.Count).ToArray(), 1, 'E');
        }

        private HashSet<Row> NotContainedCommoditiesConstraints(SourceSinkCoeffs values, Func<HashSet<CommoditySourceSink>, CommoditySourceSink, List<string>> FindCommodities, bool isSource)
        {
            var myCommodities = FindCommodities(isSource ? sources : sinks, values.source);
            var contained = commodities.Where(commo => !myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
            return contained.Select(x => new Row(CreateRow(values.rowCoeffs, x, commodities.Count).ToArray(), 0, 'E')).ToHashSet();
        }

        //record used to store the data of a given source/sink.
        private record SourceSinkCoeffs(CommoditySourceSink source, List<double> rowCoeffs);
        //Static private func section;
        //This methods are used to initialize the source and sinks constraints.
        private static readonly Func<HashSet<CommoditySourceSink>, CommoditySourceSink, List<string>> FindCommoditySource = (sources, source) => sources.Where(x => x.Name == source.Name).Select(xx => xx.Commodity).ToList();
        private static readonly Func<HashSet<CommoditySourceSink>, CommoditySourceSink, List<string>> FindCommoditySink = (sinks, sink) => sinks.Where(x => x.Name == sink.Name).Select(xx => xx.Commodity).ToList();
        private static readonly Func<Edge, string, bool> FindSources = (edge, name) => edge.Source == name;
        private static readonly Func<Edge, string, bool> FindSinks = (edge, name) => edge.Destination == name;
        private static readonly Func<int, int, int, int> ComputeIndexInEdgeResult = (edgeIndex, commodity, totalEdges) => edgeIndex + totalEdges * commodity;
        protected static readonly Func<int, List<double>> RepeatedZeroList = length => Enumerable.Repeat(0.0, length).ToList();
        protected static readonly Func<int, HashSet<int>> RangeList = length => Enumerable.Range(0, length).ToHashSet();
    }
}