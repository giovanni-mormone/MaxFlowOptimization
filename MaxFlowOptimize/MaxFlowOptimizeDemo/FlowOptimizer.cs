using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private JsonProblem originalProblem;
        private readonly IFlowProblem flow;
        private Result actualResult;
        private bool isFirstFormulation;
        private bool initialized = false;
        private Dictionary<string, List<String>> commodityGroups;
        /// <summary>
        /// Constructor of a flow optimizer;
        /// </summary>
        /// <param name="problemName">The name of the problem, param needed by the <see cref="WrapperCoinMP"/> wrapper</param>
        /// <param name="Flow"> The <see cref="IFlowProblem"/> implementation used to optimize the problem.</param>
        public FlowOptimizer(string problemName, IFlowProblem Flow, bool IsFirstFormulation)
        {
            WrapperCoin.InitSolver();
            problem = WrapperCoin.CreateProblem(problemName);
            flow = Flow;
            isFirstFormulation = IsFirstFormulation;
        }
        public void AddCommodity(string Commodity) => actualProblem.Commodities.Add(Commodity);

        public void AddEdge(Edge Edge)
        {
            if ((actualProblem.Nodes.Contains(Edge.Source) || actualProblem.CommoditiesSources.Any(x => x.Name == Edge.Source)) &&
                (actualProblem.Nodes.Contains(Edge.Destination) || actualProblem.CommoditiesSinks.Any(x => x.Name == Edge.Destination)))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                    actualProblem.Edges.Append(Edge).ToHashSet());
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void AddPenalityEdge(Edge Edge)
        {
            actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                actualProblem.Edges.Append(Edge).ToHashSet());
            flow.AddPenality(Edge);
            if (initialized)
            {
                RecreateProblem();
            }
        }

        public void UpdateEdge(Edge Edge)
        {
            if (actualProblem.Edges.Contains(Edge))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                    actualProblem.Edges.Select(edge => edge.Equals(Edge) ? Edge : edge).ToHashSet());
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        private void updateNodeEdges(string Node, HashSet<Edge> Edges) => actualProblem = new JsonProblem(actualProblem.Nodes.Append(Node).ToHashSet(),
                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks, actualProblem.Edges.Concat(Edges).ToHashSet());


        public void AddNode(string Name, HashSet<Edge> Edges)
        {
            if (!actualProblem.Nodes.Contains(Name))
            {
                updateNodeEdges(Name, Edges);
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void AddSink(CommoditySourceSink Sink)
        {
            if (actualProblem.CommoditiesSinks.All(x => !Sink.Equals(x)) && actualProblem.Commodities.Contains(Sink.Commodity))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Append(Sink).ToHashSet(),
                       actualProblem.Edges);
                if (initialized)
                {
                    RecreateProblem();
                }
            }

        }

        public void AddSource(CommoditySourceSink Source)
        {
            if (actualProblem.CommoditiesSources.All(x => !Source.Equals(x)) && actualProblem.Commodities.Contains(Source.Commodity))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources.Append(Source).ToHashSet(), actualProblem.CommoditiesSinks, actualProblem.Edges);
                if (initialized)
                {
                    RecreateProblem();
                }
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
            actualResult = flow.CreateResult(result, edgesV.ToList());
            return actualResult;
        }

        public void PrintProblemRows()
        {
            Console.WriteLine(CreateVariableNames());
            Console.WriteLine("MAX," + string.Join(",", flow.GetObjectiveCoeffs().Select(x => $"{x}").ToArray()));
            flow.GetRows().ToList().ForEach(x => Console.WriteLine(x));
        }

        public void ReadFromJSON(string path)
        {
            var serializer = new JsonSerializer();
            using StreamReader file = File.OpenText(path);
            using JsonTextReader reader = new JsonTextReader(file);
            var jsonProblem = serializer.Deserialize<JsonProblem>(reader);
            originalProblem = new JsonProblem(jsonProblem.Nodes.Select(x => x).ToHashSet(), jsonProblem.Commodities.Select(x => x).ToHashSet(),
                jsonProblem.CommoditiesSources.Select(x => x).ToHashSet(), jsonProblem.CommoditiesSinks.Select(x => x).ToHashSet(), jsonProblem.Edges.Select(x => x).ToHashSet());
            actualProblem = jsonProblem;


            if (isFirstFormulation)
            {
                modifyProblem();
                flow.InizializeProblem(actualProblem, commodityGroups);
            }
            else
            {
                actualProblem.CommoditiesSources.ToList().ForEach(source =>
                {
                    actualProblem.CommoditiesSinks.ToList().ForEach(sink =>
                    {
                        AddPenalityEdge(new Edge(4, source.Name, sink.Name));
                    });
                });
                flow.InizializeProblemAlternativeFormulation(actualProblem);
            }

            initialized = true;
            InitializeWrapperProblem(actualProblem, false);
        }

        public void LagrangianTest()
        {
            flow.CreateLagrangian(actualProblem, commodityGroups);
            InitializeWrapperProblem(actualProblem, true);
        }
        private void modifyProblem()
        {
            commodityGroups = new();

            actualProblem.Commodities.ToList().ForEach(commodity =>
            {
                var sinks = actualProblem.CommoditiesSinks.Where(sink => sink.Commodity == commodity).ToList();
                var source = actualProblem.CommoditiesSources.First(source => source.Commodity == commodity);
                List<String> associatedCommo = new();
                sinks.ForEach(sink =>
                {
                    string newCommodity = source.Name+sink.Name+commodity;
                    string dummySink = sink.Name + commodity;
                    AddCommodity(newCommodity);
                    AddSource(new CommoditySourceSink(source.Name, newCommodity, source.Capacity));
                    AddSink(new CommoditySourceSink(dummySink, newCommodity, -1));
                    var oldEdges = actualProblem.Edges.Where(edge => edge.Destination == sink.Name || edge.Source == sink.Name).ToHashSet();
                    RemoveSink(sink.Name, commodity);
                    updateNodeEdges(sink.Name, oldEdges);
                    AddEdge(new Edge(-1, sink.Name, dummySink));
                    AddPenalityEdge(new Edge(-1, source.Name, dummySink));
                    associatedCommo.Add(newCommodity);
                });
                RemoveSource(source.Name, source.Commodity);
                RemoveCommodity(commodity);
                commodityGroups.Add(commodity, associatedCommo);
            });
        }

        public void RemoveCommodity(string Commodity)
        {
            if (actualProblem.Commodities.Remove(Commodity))
            {
                CheckIfLastCommoditySource(Commodity);
                CheckIfLastCommoditySink(Commodity);
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources.Where(x => !x.Commodity.Equals(Commodity)).ToHashSet(),
                actualProblem.CommoditiesSinks.Where(x => !x.Commodity.Equals(Commodity)).ToHashSet(), actualProblem.Edges);
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveEdge(Edge Edge)
        {
            if (actualProblem.Edges.Contains(Edge))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                   actualProblem.Edges.Where(x => !x.Equals(Edge)).ToHashSet());
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveNode(string Name)
        {
            if (actualProblem.Nodes.Contains(Name))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes.Where(x => !x.Equals(Name)).ToHashSet(),
                                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                                actualProblem.Edges.Where(x => !x.Source.Equals(Name) && !x.Destination.Equals(Name)).ToHashSet());
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveSource(string Source, string Commodity)
        {
            CommoditySourceSink toRemove = new CommoditySourceSink(Source, Commodity);
            if (actualProblem.CommoditiesSources.Contains(toRemove))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources.Where(x => !x.Equals(toRemove)).ToHashSet(),
                    actualProblem.CommoditiesSinks, actualProblem.Edges);
                //check
                CheckIfLastCommoditySource(Source);
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveSink(string Sink, string Commodity)
        {
            CommoditySourceSink toRemove = new CommoditySourceSink(Sink, Commodity);
            if (actualProblem.CommoditiesSinks.Contains(toRemove))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Where(x => !x.Equals(toRemove)).ToHashSet(),
                    actualProblem.Edges);
                //check
                CheckIfLastCommoditySink(Sink);
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void SaveMPS(string path) => WrapperCoin.WriteMPSFile(problem, path);

        public void SaveCSV(string path)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(CreateVariableNames());
            var xx = string.Join(",", flow.GetObjectiveCoeffs().Select(x => $"{x}").ToArray()) + ", MAX";
            output.AppendLine(xx);
            flow.GetRows().ToList().ForEach(x => output.AppendLine(x.ToString()));
            File.WriteAllText(path + ".csv", output.ToString());
        }

        public void SaveToJSON(string path)
        {
            var serializer = new JsonSerializer();
            using StreamWriter file = File.CreateText(path);
            using JsonTextWriter writer = new JsonTextWriter(file);
            serializer.Serialize(writer, actualProblem);
        }

        public void SaveResult(string path)
        {
            var serializer = new JsonSerializer();
            using StreamWriter file = File.CreateText(path + ".json");
            using JsonTextWriter writer = new JsonTextWriter(file);
            serializer.Serialize(writer, actualResult);
        }

        private void CheckIfLastCommoditySource(string Source)
        {
            if (actualProblem.CommoditiesSources.All(x => x.Name != Source))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                    actualProblem.Edges.Where(x => x.Source != Source).ToHashSet());
            }
        }
        private void CheckIfLastCommoditySink(string Sink)
        {
            if (actualProblem.CommoditiesSinks.All(x => x.Name != Sink))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                    actualProblem.Edges.Where(x => x.Destination != Sink).ToHashSet());
            }
        }

        private void RecreateProblem()
        {
            problem = WrapperCoin.CreateProblem(WrapperCoin.GetProblemName(problem));
            InitializeWrapperProblem(actualProblem, false);
        }
        private void InitializeWrapperProblem(JsonProblem jsonProblem, bool isLagrangian)
        {
            int numberOfVariables = jsonProblem.Edges.Count * (jsonProblem.Commodities.Count + (isFirstFormulation ? originalProblem.Commodities.Count : jsonProblem.Commodities.Count));
            double objconst = isLagrangian? -500.0 : 0;
            int objsens = WrapperCoin.SOLV_OBJSENS_MIN;
            double infinite = WrapperCoin.GetInfinity();
            List<double> lowerBounds = Enumerable.Repeat(0.0, numberOfVariables).ToList();
            List<double> upperBounds = Enumerable.Repeat(infinite, numberOfVariables).ToList();
            List<int> matrixBegin = Enumerable.Repeat(0, numberOfVariables + 1).ToList();
            List<int> matrixCount = Enumerable.Repeat(0, numberOfVariables).ToList();
            double[] n = Array.Empty<double>();
            char[] c = Array.Empty<char>();
            int[] i = Array.Empty<int>();

            double[] objectCoeffs = flow.GetObjectiveCoeffs();
            List<Row> rows = flow.GetRows().ToList();
            WrapperCoin.LoadProblem(problem, numberOfVariables, 0, 0, 0, objsens, objconst, objectCoeffs, lowerBounds.ToArray(), upperBounds.ToArray(), c, n, null, matrixBegin.ToArray(), matrixCount.ToArray(), i, n
                , null, null, "");
            rows.ForEach(x => WrapperCoin.AddRow(ref problem, x.Coeffs, x.ConstraintValue, x.ConstraintType, ""));
            List<char> columnType = Enumerable.Repeat('B', numberOfVariables).ToList();
            WrapperCoin.LoadInteger(problem, columnType.ToArray());
        }

        private string CreateVariableNames() => string.Join(",", actualProblem.Commodities.SelectMany(commodity => (actualProblem.Edges.Select(edge =>
        {
            return commodity + "_" + edge.Source + "->" + edge.Destination;
        }))).Concat(isFirstFormulation ? _createXiName() : new ()).ToList().Select(x => $"{x}")).ToString();

        private List<string> _createXiName() => originalProblem.Commodities.SelectMany(commodity => actualProblem.Edges.Select(edge =>
        {
            return "Xi" + commodity + edge.Source + "->" + edge.Destination;
        })).ToList();
    }
}