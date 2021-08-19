using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WrapperCoinMP;

namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// Basic implementation of the <see cref="IFlowOptimizer"/> interface.
    /// </summary>
    class FlowOptimizer : IFlowOptimizer
    {
        private WrapProblem problem;
        private JsonProblem actualProblem;
        private readonly IFlowProblem flow;

        public FlowOptimizer(string problemName, IFlowProblem Flow)
        {
            WrapperCoin.InitSolver();
            problem = WrapperCoin.CreateProblem(problemName);
            flow = Flow;
        }
        public void AddCommodity(string Commodity) => actualProblem.Commodities.Add(Commodity);


        public void AddCommodityToSink(string Sink, string Commodity)
        {
            if (actualProblem.Sinks.Any(x => x.Name == Sink) && actualProblem.Commodities.Contains(Commodity))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources,
                actualProblem.CommoditiesSinks.Append(new CommoditySink(Sink, Commodity)).ToHashSet(), actualProblem.Edges);
                CheckIfLastCommoditySink(Commodity);
                RecreateProblem();
            }
        }

        public void AddCommodityToSource(string Source, string Commodity)
        {
            if (actualProblem.Sources.Any(x => x.Name == Source) && actualProblem.Commodities.Contains(Commodity))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Append(new CommoditySource(Source, Commodity)).ToHashSet(),
                actualProblem.CommoditiesSinks, actualProblem.Edges);
                RecreateProblem();
            }
        }

        public void AddEdge(Edge Edge)
        {
            if ((actualProblem.Nodes.Contains(Edge.Source) || actualProblem.Sources.Any(x => x.Name == Edge.Source)) &&
                (actualProblem.Nodes.Contains(Edge.Destination) || actualProblem.Sinks.Any(x => x.Name == Edge.Destination)))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                    actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                    actualProblem.Edges.Append(Edge).ToHashSet());
                RecreateProblem();
            }
        }

        public void AddNode(string Name, HashSet<Edge> Edges)
        {
            if (!actualProblem.Nodes.Contains(Name))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes.Append(Name).ToHashSet(),
                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks, actualProblem.Edges.Concat(Edges).ToHashSet());
                RecreateProblem();
            }
        }

        public void AddSink(SinkSource Sink, List<string> Commodities)
        {
            if (actualProblem.Sinks.Any(x => x.Name != Sink.Name) && Commodities.Any(x => actualProblem.Commodities.Contains(x)))
            {
                var x = Commodities.Where(xx => actualProblem.Commodities.Contains(xx)).Select(x => new CommoditySink(Sink.Name, x));
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks.Append(Sink).ToHashSet(),
                       actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Concat(x).ToHashSet(),
                       actualProblem.Edges);
                RecreateProblem();
            }

        }

        public void AddSource(SinkSource Source, List<string> Commodities)
        {
            if (actualProblem.Sources.Any(x => x.Name != Source.Name) && Commodities.Any(x => actualProblem.Commodities.Contains(x)))
            {
                var x = Commodities.Where(xx => actualProblem.Commodities.Contains(xx)).Select(x => new CommoditySource(Source.Name, x));
                actualProblem = new JsonProblem(actualProblem.Sources.Append(Source).ToHashSet(), actualProblem.Sinks,
                       actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Concat(x).ToHashSet(), actualProblem.CommoditiesSinks,
                       actualProblem.Edges);
                RecreateProblem();
            }
        }

        public Result OptimizeProblem()
        {
            WrapperCoin.OptimizeProblem(problem);

            double result = WrapperCoin.GetObjectValue(problem);
            double[] edgesV = new double[WrapperCoin.GetColCount(problem)];
            double[] reducedCost = new double[WrapperCoin.GetColCount(problem)];
            double[] slackV = new double[WrapperCoin.GetRowCount(problem)];
            double[] shadowPrice = new double[WrapperCoin.GetRowCount(problem)];

            WrapperCoin.GetSolutionValues(problem, edgesV, reducedCost, slackV, shadowPrice);

            return flow.CreateResult(result, edgesV.ToList());
        }

        public void ReadFromJSON(string path)
        {
            var serializer = new JsonSerializer();
            //../../../problem.json se parte da visual studio per ora
            using StreamReader file = File.OpenText(path);
            using JsonTextReader reader = new JsonTextReader(file);
            var jsonProblem = serializer.Deserialize<JsonProblem>(reader);
            actualProblem = jsonProblem;
            InitializeWrapperProblem(jsonProblem);
        }

        public void RemoveCommodity(string Commodity)
        {
            if (actualProblem.Commodities.Remove(Commodity))
            {
                CheckIfLastCommoditySource(Commodity);
                CheckIfLastCommoditySink(Commodity);
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Where(x => !x.Commodity.Equals(Commodity)).ToHashSet(),
                actualProblem.CommoditiesSinks.Where(x => !x.Commodity.Equals(Commodity)).ToHashSet(), actualProblem.Edges);
                RecreateProblem();
            }
        }

        public void RemoveCommodityFromSink(string Sink, string Commodity)
        {
            if (actualProblem.CommoditiesSinks.Any(x => x.Commodity == Commodity && x.Sink == Sink))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources,
                actualProblem.CommoditiesSinks.Where(x => !(x.Sink == Sink && x.Commodity == Commodity)).ToHashSet(), actualProblem.Edges);
                RecreateProblem();
            }
        }

        public void RemoveCommodityFromSource(string Source, string Commodity)
        {
            if (actualProblem.CommoditiesSources.Any(x => x.Commodity == Commodity && x.Source == Source))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Where(x => !(x.Source == Source && x.Commodity == Commodity)).ToHashSet(),
                actualProblem.CommoditiesSinks, actualProblem.Edges);
                CheckIfLastCommoditySource(Commodity);
                RecreateProblem();
            }
        }

        public void RemoveEdge(Edge Edge)
        {
            if (actualProblem.Edges.Contains(Edge))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                   actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                   actualProblem.Edges.Where(x => !x.Equals(Edge)).ToHashSet());
                RecreateProblem();
            }
        }

        public void RemoveNode(string Name)
        {
            if (actualProblem.Nodes.Contains(Name))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                                actualProblem.Nodes.Where(x => !x.Equals(Name)).ToHashSet(),
                                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                                actualProblem.Edges.Where(x => !x.Source.Equals(Name) && !x.Destination.Equals(Name)).ToHashSet());
                RecreateProblem();
            }
        }

        public void RemoveSink(string Sink)
        {
            if (actualProblem.Sinks.Any(x => x.Name == Sink))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks.Where(x => !x.Name.Equals(Sink)).ToHashSet(),
                 actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Where(x => !x.Sink.Equals(Sink)).ToHashSet(),
                 actualProblem.Edges.Where(x => !x.Destination.Equals(Sink)).ToHashSet());
                RecreateProblem();
            }
        }

        public void RemoveSource(string Source)
        {
            if (actualProblem.Sources.Any(x => x.Name == Source))
            {
                actualProblem = new JsonProblem(actualProblem.Sources.Where(x => !x.Name.Equals(Source)).ToHashSet(), actualProblem.Sinks,
                 actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Where(x => !x.Source.Equals(Source)).ToHashSet(), actualProblem.CommoditiesSinks,
                 actualProblem.Edges.Where(x => !x.Source.Equals(Source)).ToHashSet());
                RecreateProblem();
            }
        }

        public void SaveMPS(string path) => WrapperCoin.WriteMPSFile(problem, path);


        public void SaveToJSON(string path)
        {
            var serializer = new JsonSerializer();
            using StreamWriter file = File.CreateText(path);
            using JsonTextWriter writer = new JsonTextWriter(file);
            serializer.Serialize(writer, actualProblem);
        }

        private void CheckIfLastCommoditySource(string Commodity)
        {
            actualProblem.Sources.ToList().ForEach(x => {
                if (!actualProblem.CommoditiesSources.Any(xx => xx.Source == x.Name && xx.Commodity != Commodity))
                {
                    RemoveSource(x.Name);
                }
            });
        }
        private void CheckIfLastCommoditySink(string Commodity)
        {
            actualProblem.Sinks.ToList().ForEach(x =>
            {
                if (!actualProblem.CommoditiesSinks.Any(xx => xx.Sink == x.Name && xx.Commodity != Commodity))
                {
                    RemoveSink(x.Name);
                }
            });
        }

        private void RecreateProblem()
        {
            problem = WrapperCoin.CreateProblem(WrapperCoin.GetProblemName(problem));
            InitializeWrapperProblem(actualProblem);
        }

        private void InitializeWrapperProblem(JsonProblem jsonProblem)
        {
            int numberOfVariables = jsonProblem.Edges.Count * jsonProblem.Commodities.Count;
            double objconst = 0.0;
            int objsens = WrapperCoin.SOLV_OBJSENS_MAX;
            double infinite = WrapperCoin.GetInfinity();
            List<double> lowerBounds = Enumerable.Repeat(0.0, numberOfVariables).ToList();
            List<double> upperBounds = Enumerable.Repeat(infinite, numberOfVariables).ToList();
            List<int> matrixBegin = Enumerable.Repeat(0, numberOfVariables + 1).ToList();
            List<int> matrixCount = Enumerable.Repeat(0, numberOfVariables).ToList();
            double[] n = Array.Empty<double>();
            char[] c = Array.Empty<char>();
            int[] i = Array.Empty<int>();

            flow.InizializeProblem(actualProblem);
            double[] objectCoeffs = flow.GetObjectiveCoeffs();
            List<Row> rows = flow.GetRows().ToList();
            WrapperCoin.LoadProblem(problem, numberOfVariables, 0, 0, 0, objsens, objconst, objectCoeffs, lowerBounds.ToArray(), upperBounds.ToArray(), c, n, null, matrixBegin.ToArray(), matrixCount.ToArray(), i, n
                , null, null, "");
            rows.ForEach(x => WrapperCoin.AddRow(ref problem, x.Coeffs, x.ConstraintValue, x.ConstraintType, ""));
        }

    }
}
