﻿using MaxFlowOptimizeDemo.jsonStructures;
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
    /// </summary>
    public abstract class AbstractFlowProblem : IFlowProblem
    {
        protected static readonly double INFINITY = WrapperCoinMP.WrapperCoin.GetInfinity();
        protected HashSet<string> nodes;
        protected HashSet<CommoditySourceSink> sources;
        protected HashSet<CommoditySourceSink> sinks;
        protected List<double> objectiveCoeffs;
        protected HashSet<Edge> edges;
        protected HashSet<Row> rows;
        protected HashSet<Commodity> commodities;


        /// <summary>
        /// Method used to initialize a <see cref="IFlowProblem"/>.
        /// </summary>
        /// <param name="loadedProblem">The <see cref="JsonProblem"/> to initialize.</param>
        public void InizializeProblem(JsonProblem loadedProblem)
        {
            InitializeNodesAndEdges(loadedProblem);
            InitializeObjectiveCoeffs(loadedProblem);
            InitializeRows(loadedProblem);

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

       
        protected abstract void InitializeObjectiveCoeffs(JsonProblem loadedProblem);

        protected virtual void InitializeRows(JsonProblem loadedProblem)
        {

            rows = SourceSinkInitialize(loadedProblem, true);
            rows = rows.Concat(SourceSinkInitialize(loadedProblem, false)).ToHashSet();

            rows = rows.Concat(nodes.SelectMany(node =>
                edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node && !sinks.Select(x => x.Name).Contains(combo.edge.Destination))
                    .Select(edge =>
                        new Row(RangeList(loadedProblem.Commodities.Count).SelectMany(_ => RepeatedZeroList(loadedProblem.Edges.Count)
                            .Select((value, column) => column == edge.column ? 1 : value).ToList()).ToArray(), edge.edge.Weigth, 'L')
                )
            )).ToHashSet();
        }
        

        protected static List<double> CreateRow(List<double> coeffs, int commodity, int totalCommodities)
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


        private void InitializeNodesAndEdges(JsonProblem loaded)
        {
            commodities = loaded.Commodities.Select((name, id) => (name, id)).Select(x => new Commodity(x.name, x.id)).ToHashSet();
            sources = loaded.CommoditiesSources.ToHashSet();
            sinks = loaded.CommoditiesSinks.ToHashSet();
            nodes = loaded.Nodes.ToHashSet();
            edges = RangeList(loaded.Edges.Count).Select(x => loaded.Edges.ElementAt(x)).ToHashSet();
        } 
        //Method used to compute, in a tail recursive way, the row constraints of sources and sinks;
        //It uses the loaded problem, a boolean to decide if it is source or sink,
        private HashSet<Row> SourceSinkInitialize(JsonProblem loadedProblem, bool IsSource)
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
                          rowRecursive.Append(ContainedCommodityConstraint(Metodino(loadedProblem, source, findSourceSink)))
                          .Concat(NotContainedCommoditiesConstraints(Metodino(loadedProblem, source, findSourceSink), findCommoditySourceSink)).ToHashSet(),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name == tail.First().Name =>
                        _SourceSinkInitialize(tail, rowRecursive.Append(ContainedCommodityConstraint(Metodino(loadedProblem, source, findSourceSink))).ToHashSet()),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name != tail.First().Name =>
                        _SourceSinkInitialize(tail, rowRecursive.Append(ContainedCommodityConstraint(Metodino(loadedProblem, source, findSourceSink)))
                        .Concat(NotContainedCommoditiesConstraints(Metodino(loadedProblem, source, findSourceSink), findCommoditySourceSink)).ToHashSet()),
                _ => rowRecursive,
            };
            return _SourceSinkInitialize(IsSource ? sources.OrderBy(x => x.Name).ToList() : sinks.OrderBy(x => x.Name).ToList(), new());
        }

       //method used to create the SourceSinkCoeffs for the given CommoditySourceSink.
        private SourceSinkCoeffs Metodino(JsonProblem loadedProblem, CommoditySourceSink source, Func<Edge, string, bool> findSourceSink)
        {
            var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(edge => findSourceSink(edge.edge, source.Name)).ToList();
            var rowCoeffs = RepeatedZeroList(loadedProblem.Edges.Count);
            sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
            return new SourceSinkCoeffs(loadedProblem, source, rowCoeffs);
        }
        //Method that create the constraint for a given source or sink.
        private Row ContainedCommodityConstraint(SourceSinkCoeffs values)
        {
            var contained = commodities.First(commo => commo.CommodityName == values.source.Commodity).CommodityNumber;
            double weight = values.source.Capacity == -1 ? INFINITY : values.source.Capacity;
            return new Row(CreateRow(values.rowCoeffs, contained, values.loadedProblem.Commodities.Count).ToArray(), weight, 'L');

        }

        //method that create the constraints for all the commodities that are not present in the given source or sink.
        private HashSet<Row> NotContainedCommoditiesConstraints(SourceSinkCoeffs values, Func<JsonProblem, CommoditySourceSink, List<string>> FindCommodities)
        {
            var myCommodities = FindCommodities(values.loadedProblem, values.source);
            var contained = commodities.Where(commo => !myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
            return contained.Select(x => new Row(CreateRow(values.rowCoeffs, x, values.loadedProblem.Commodities.Count).ToArray(), 0, 'L')).ToHashSet();
        }

        //record used to store the data of a given source/sink.
        private record SourceSinkCoeffs(JsonProblem loadedProblem, CommoditySourceSink source, List<double> rowCoeffs);
        //Static private func section;
        //This methods are used to initialize the source and sinks constraints.
        private static readonly Func<JsonProblem, CommoditySourceSink, List<string>> FindCommoditySource = (loadproblem, source) => loadproblem.CommoditiesSources.Where(x => x.Name == source.Name).Select(xx => xx.Commodity).ToList();
        private static readonly Func<JsonProblem, CommoditySourceSink, List<string>> FindCommoditySink = (loadproblem, sink) => loadproblem.CommoditiesSinks.Where(x => x.Name == sink.Name).Select(xx => xx.Commodity).ToList();
        private static readonly Func<Edge, string, bool> FindSources = (edge, name) => edge.Source == name;
        private static readonly Func<Edge, string, bool> FindSinks = (edge, name) => edge.Destination == name;
        private static readonly Func<int, int, int, int> ComputeIndexInEdgeResult = (edgeIndex, commodity, totalEdges) => edgeIndex + totalEdges * commodity;
        protected static readonly Func<int, List<double>> RepeatedZeroList = length => Enumerable.Repeat(0.0, length).ToList();
        protected static readonly Func<int, HashSet<int>> RangeList = length => Enumerable.Range(0, length).ToHashSet();
    }
}
