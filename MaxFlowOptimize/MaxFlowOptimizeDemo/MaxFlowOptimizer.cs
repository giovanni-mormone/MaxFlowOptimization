using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WrapperCoinMP;

namespace MaxFlowOptimizeDemo
{
    class MaxFlowOptimizer : IFlowOptimizer
    {
        private WrapProblem problem;
        private FlowProblem loadedProblem;
        private JsonProblem actualProblem;


        public MaxFlowOptimizer(string problemName)
        {
            WrapperCoin.InitSolver();
            problem = WrapperCoin.CreateProblem(problemName);
        }


        public void SaveToJSON(string path)
        {
            var serializer = new JsonSerializer();
            StreamWriter file = File.CreateText(path);
            JsonTextWriter writer = new JsonTextWriter(file);
            serializer.Serialize(writer, actualProblem);
            file.Close();
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
            file.Close();
        }

        public void AddNode(string Name, HashSet<Edge> Edges)
        {
            actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes.Append(Name).ToHashSet(),
                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks, actualProblem.Edges.Concat(Edges).ToHashSet());
            RecreateProblem();
        }

        public void RemoveNode(string Name)
        {
            if (actualProblem.Nodes.Contains(Name))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                                actualProblem.Nodes.Where( x => !x.Equals(Name)).ToHashSet(),
                                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                                actualProblem.Edges.Where(x => !x.Source.Equals(Name) && !x.Destination.Equals(Name)).ToHashSet());
                RecreateProblem();
            }
        }

        public void AddEdge(Edge Edge)
        {
            if ((actualProblem.Nodes.Contains(Edge.Source) || actualProblem.Sources.Select(x => x.Name).Contains(Edge.Source) && actualProblem.Nodes.Contains(Edge.Destination)))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                    actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                    actualProblem.Edges.Append(Edge).ToHashSet());
                RecreateProblem();
            }
        }
        public void RemoveEdge(Edge Edge)
        {
            if (actualProblem.Edges.Contains(Edge))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                   actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                   actualProblem.Edges.Where( x => !x.Equals(Edge)).ToHashSet());
                RecreateProblem();
            }


        }

        public void AddSource(SinkSource Source, List<string> Commodities)
        {
            var x = Commodities.Where(xx => actualProblem.Commodities.Contains(xx)).Select(x => new CommoditySource(Source.Name, x));
            actualProblem = new JsonProblem(actualProblem.Sources.Append(Source).ToHashSet(), actualProblem.Sinks,
                   actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Concat(x).ToHashSet(), actualProblem.CommoditiesSinks,
                   actualProblem.Edges);
            RecreateProblem();
        }
        public void RemoveSource(string source)
        {
            if (actualProblem.Sources.Select(x => x.Name).Contains(source))
            {
                actualProblem = new JsonProblem(actualProblem.Sources.Where(x => !x.Name.Equals(source)).ToHashSet(), actualProblem.Sinks,
                 actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Where(x => !x.Source.Equals(source)).ToHashSet(), actualProblem.CommoditiesSinks,
                 actualProblem.Edges.Where(x => !x.Source.Equals(source)).ToHashSet());
                RecreateProblem();
            }
        }
        public void AddSink(SinkSource Sink, List<string> Commodities)
        {
            var x = Commodities.Where(xx => actualProblem.Commodities.Contains(xx)).Select(x => new CommoditySink(Sink.Name, x));
            actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks.Append(Sink).ToHashSet(),
                   actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Concat(x).ToHashSet(),
                   actualProblem.Edges);
            RecreateProblem();
        }
        public void RemoveSink(string sink)
        {
            if (actualProblem.Sinks.Select(x => x.Name).Contains(sink))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks.Where(x => !x.Name.Equals(sink)).ToHashSet(),
                 actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Where(x => !x.Sink.Equals(sink)).ToHashSet(),
                 actualProblem.Edges.Where(x => !x.Destination.Equals(sink)).ToHashSet());
                RecreateProblem();
            }
        }
        public void AddCommodity(string Commodity) => actualProblem.Commodities.Add(Commodity);

        public void RemoveCommodity(string Commodity)
        {
            if (actualProblem.Commodities.Remove(Commodity)){
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Where(x => !x.Commodity.Equals(Commodity)).ToHashSet(),
                actualProblem.CommoditiesSinks.Where(x => !x.Commodity.Equals(Commodity)).ToHashSet(), actualProblem.Edges);
                RecreateProblem();
            }
        }

        public void AddCommodityToSource(string Source, string Commodity)
        {
            if(actualProblem.Sources.Select(x => x.Name).Contains(Source) && actualProblem.Commodities.Contains(Commodity))
            {
                actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Append(new CommoditySource(Source, Commodity)).ToHashSet(),
                actualProblem.CommoditiesSinks.Append(new CommoditySink(Source, Commodity)).ToHashSet(), actualProblem.Edges);
                RecreateProblem();
            }
        }
        public void RemoveCommodityFromSource(string Source, string Commodity)
        {

        }

        private void RecreateProblem()
        {
            problem = WrapperCoin.CreateProblem(WrapperCoin.GetProblemName(problem));
            InitializeWrapperProblem(actualProblem);
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

            return loadedProblem.CreateResult(result, edgesV.ToList());
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
            loadedProblem = FlowProblem.InizializeProblem(jsonProblem);
            double[] objectCoeffs = loadedProblem.GetObjectiveCoeffs();

            double[] n = Array.Empty<double>();
            char[] c = Array.Empty<char>();
            int[] i = Array.Empty<int>();
            List<Row> rows = loadedProblem.GetRows().ToList();
            WrapperCoin.LoadProblem(problem, numberOfVariables, 0, 0, 0, objsens, objconst, objectCoeffs, lowerBounds.ToArray(), upperBounds.ToArray(), c, n, null, matrixBegin.ToArray(), matrixCount.ToArray(), i, n
                , null, null, "");
            rows.ForEach(x => WrapperCoin.AddRow(ref problem, x.Coeffs, x.ConstraintValue, x.ConstraintType, ""));
        }
        
    }
}
