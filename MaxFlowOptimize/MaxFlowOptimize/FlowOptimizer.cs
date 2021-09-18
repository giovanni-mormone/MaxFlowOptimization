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
        //private variables of the optimizer;

        private static readonly int LAMBDA = 5;
        private WrapProblem problem;
        //this 2 variables represent the actual state of the problem(e.g after adding nodes, edges, sinks or any modification
        //to it) and the original problem loaded from the json file.
        private JsonProblem actualProblem;
        private JsonProblem originalProblem;
        private readonly IFlowProblem flow;
        private Result actualResult;
        //variable used to specify the matematical formulation utilized; if false utilizes the formulation of prof. Boschetti.
        private bool isFirstFormulation;
        //variable used to decide wheter or not to call recreate problem after the methods that modify the problem;
        //it is usefull because when loading the problem for the first formulation, we need to modifiy the graph and not to
        //recreate the problem each time.
        private bool initialized = false;
        //this dictionary is used to store the original commodities as keys associated to a list of the new commodities
        //generated when loading the problem for the first formulation
        private Dictionary<string, List<String>> commodityGroups = new();


        /// <summary>
        /// Constructor of a flow optimizer;
        /// </summary>
        /// <param name="problemName">The name of the problem, param needed by the <see cref="WrapperCoinMP"/> wrapper</param>
        /// <param name="Flow"> The <see cref="IFlowProblem"/> implementation used to optimize the problem.</param>
        /// <param name="IsFirstFormulation"> The <see cref="bool"/> telling which formulation to use</param>
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
            //the condition tells if the nodes specified in the edge to add are present in the problem
            if ((actualProblem.Nodes.Contains(Edge.Source) || actualProblem.CommoditiesSources.Any(x => x.Name == Edge.Source)) &&
                (actualProblem.Nodes.Contains(Edge.Destination) || actualProblem.CommoditiesSinks.Any(x => x.Name == Edge.Destination)))
            {
                //there is no need to check if the edge is already present, the structure is an HashSet that does not have duplicates 
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
            //simply add a new edge to the problem, no need to to check because it is an HashSet;
            actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                actualProblem.Edges.Append(Edge).ToHashSet());
            //adding the edge to the collection of penality edges in the flowoptimizer utilized.
            flow.AddPenality(Edge);
            if (initialized)
            {
                RecreateProblem();
            }
        }

        public void UpdateEdge(Edge Edge)
        {
            //an edge is updated only if present
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

        //method used to update the list of edges associated to a node; it can also add a new node and its associated edges;
        //it works both ways becauase the nodes are stored in an hashset that cannot have duplicates.
        private void UpdateNodeEdges(string Node, HashSet<Edge> Edges) => actualProblem = new JsonProblem(actualProblem.Nodes.Append(Node).ToHashSet(),
                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks, actualProblem.Edges.Concat(Edges).ToHashSet());


        public void AddNode(string Name, HashSet<Edge> Edges)
        {
            //i can add a new node only if it is not already in the problem
            if (!actualProblem.Nodes.Contains(Name))
            {
                UpdateNodeEdges(Name, Edges);
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void AddSink(CommoditySourceSink Sink)
        {
            //i can only add a new sink if it is not present and the commodity associated is present
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
            //i can only add a new source if it is not present and the commodity associated is present

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
            Console.WriteLine("MIN," + string.Join(",", flow.GetObjectiveCoeffs().Select(x => $"{x}").ToArray()));
            flow.GetRows().ToList().ForEach(x => Console.WriteLine(x));
        }

        public void ReadFromJSON(string path)
        {
            //file reading section
            var serializer = new JsonSerializer();
            using StreamReader file = File.OpenText(path);
            using JsonTextReader reader = new JsonTextReader(file);
            var jsonProblem = serializer.Deserialize<JsonProblem>(reader);
            //after the file reading, it saves the loaded problem in a new structure to keep the original problem
            originalProblem = new JsonProblem(jsonProblem.Nodes.Select(x => x).ToHashSet(), jsonProblem.Commodities.Select(x => x).ToHashSet(),
                jsonProblem.CommoditiesSources.Select(x => x).ToHashSet(), jsonProblem.CommoditiesSinks.Select(x => x).ToHashSet(), jsonProblem.Edges.Select(x => x).ToHashSet());
            //then it sets the problem to the actual problem
            actualProblem = jsonProblem;

            if (isFirstFormulation)
            {
                //if it enters here, it should modify the problem according to the first formulation.
                modifyProblem();
                flow.InizializeProblem(actualProblem, commodityGroups);
            }
            else
            {
                //for the second formulation it's needed to add a penality edge between sources and sinks.
                actualProblem.CommoditiesSources.ToList().ForEach(source =>
                {
                    actualProblem.CommoditiesSinks.ToList().ForEach(sink =>
                    {
                        AddPenalityEdge(new Edge(4, source.Name, sink.Name));
                    });
                });
                flow.InizializeProblemAlternativeFormulation(actualProblem);
            }
            //after the problem initialization, it sets the initialized variable to true and initialazes the wrapper, 
            initialized = true;
            //it has false as second parameter because it is not a lagrangian optimization
            InitializeWrapperProblem(actualProblem, false);
        }

        public void LagrangianOptimization()
        {
            //simply initializes the problem in lagrangian form for the first formulation;
            //it needs an initialized flow with the first formulation;
            if(isFirstFormulation && initialized)
            {
                flow.CreateLagrangian(actualProblem, commodityGroups, LAMBDA);
                InitializeWrapperProblem(actualProblem, true);
            }
        }
        private void modifyProblem()
        {
            //method used to create the new commodities in the form of couples source/sink; 
            //it also adds a dummy sink for each new commodity and a penality edge between source and dummy sinks.
            actualProblem.Commodities.ToList().ForEach(commodity =>
            {
                //gets the sinks that require a commodity and the source of the commodity
                var sinks = actualProblem.CommoditiesSinks.Where(sink => sink.Commodity == commodity).ToList();
                var source = actualProblem.CommoditiesSources.First(source => source.Commodity == commodity);
                //initilizes a new list  that will be added to the dictionary keeping track of original and new commodities.
                List<string> associatedCommo = new();
                
                sinks.ForEach(sink =>
                {
                    //creation of the new commodity and dummy sink
                    string newCommodity = source.Name+sink.Name+commodity;
                    string dummySink = sink.Name + commodity;
                    //addition of the commodity, a new source and sink to the problem
                    AddCommodity(newCommodity);
                    AddSource(new CommoditySourceSink(source.Name, newCommodity, source.Capacity));
                    AddSink(new CommoditySourceSink(dummySink, newCommodity, -1));
                    //this is needed because we have to remove the old sink from the sink list and re-add it to the problem
                    //as a node; if we remove a sink and it not requires other commodities, the method remove sink also removes
                    //the edges associated to it
                    var oldEdges = actualProblem.Edges.Where(edge => edge.Destination == sink.Name || edge.Source == sink.Name).ToHashSet();
                    RemoveSink(new CommoditySourceSink(sink.Name, commodity));
                    //readding the sink to the problem as a new node and adding a new edge to the problem between source and 
                    //dummy sink, specifying that it is a penality edge
                    UpdateNodeEdges(sink.Name, oldEdges);
                    AddEdge(new Edge(-1, sink.Name, dummySink));
                    AddPenalityEdge(new Edge(-1, source.Name, dummySink));
                    //adding the new commodity to the list initialized before
                    associatedCommo.Add(newCommodity);
                });
                //after a commodity is computed, i remove it from the commodities(the method also removes any source and sink 
                //associated to the removed commodity),
                //adding the commodity to the dictionary of old and new commodities
                RemoveCommodity(commodity);
                commodityGroups.Add(commodity, associatedCommo);
            });
        }

        public void RemoveCommodity(string Commodity)
        {
            //if the commodity is present it is removed and performs the internal checks
            if (actualProblem.Commodities.Remove(Commodity))
            {
                actualProblem.CommoditiesSources.Where(source => source.Commodity == Commodity).ToList().ForEach(source =>
                    RemoveSource(source));
                actualProblem.CommoditiesSinks.Where(sink => sink.Commodity == Commodity).ToList().ForEach(sink =>
                    RemoveSink(sink));
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveEdge(Edge Edge)
        {
            //only removes and edge if present
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
            //only removes a node if present
            if (actualProblem.Nodes.Contains(Name))
            {
                //it removes all the edges that are from or to the removed node
                actualProblem = new JsonProblem(actualProblem.Nodes.Where(x => !x.Equals(Name)).ToHashSet(),
                                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks,
                                actualProblem.Edges.Where(x => !x.Source.Equals(Name) && !x.Destination.Equals(Name)).ToHashSet());
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveSource(CommoditySourceSink toRemove)
        {
            if (actualProblem.CommoditiesSources.Contains(toRemove))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources.Where(x => !x.Equals(toRemove)).ToHashSet(),
                    actualProblem.CommoditiesSinks, actualProblem.Edges);
                //check if the commodity-source removed is the last one generated by the source; if so, it should remove 
                //the edges associated to it
                CheckIfLastCommoditySource(toRemove.Name);
                if (initialized)
                {
                    RecreateProblem();
                }
            }
        }

        public void RemoveSink(CommoditySourceSink toRemove)
        {
            if (actualProblem.CommoditiesSinks.Contains(toRemove))
            {
                actualProblem = new JsonProblem(actualProblem.Nodes, actualProblem.Commodities,
                    actualProblem.CommoditiesSources, actualProblem.CommoditiesSinks.Where(x => !x.Equals(toRemove)).ToHashSet(),
                    actualProblem.Edges);
                //check if the commodity-sink removed is the last one consumed by the sink; if so, it should remove 
                //the edges associated to it
                CheckIfLastCommoditySink(toRemove.Name);
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
            var xx = string.Join(",", flow.GetObjectiveCoeffs().Select(x => $"{x}").ToArray()) + ", MIN";
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

        ////////////////////////////////////////////
        //methods that remove all the edges associated to a source/sink that has no more commodities
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

        ////////////////////////////////////////////

        //simple recreation of the problem after it is modified
        private void RecreateProblem()
        {
            problem = WrapperCoin.CreateProblem(WrapperCoin.GetProblemName(problem));
            InitializeWrapperProblem(actualProblem, false);
        }


        private void InitializeWrapperProblem(JsonProblem jsonProblem, bool isLagrangian)
        {
            //the number of variable is calculated for each formulation: the first formulation add a number of Xi variables equal
            //to the original number of commodities for edge in the problem that is saved in the originalproblem variable, the second formulation
            //add a number of Xi equal to the actual number of commodities for edge(it does not modify the problem like the first formulation)
            int numberOfVariables = jsonProblem.Edges.Count * (jsonProblem.Commodities.Count + (isFirstFormulation ? originalProblem.Commodities.Count : jsonProblem.Commodities.Count));
            
            //if it is using a lagrangian formulation for the first formulation, there is a cost to remove associated to 
            //each edge capacity and a lambda -> !!!!the lambda is a random value in this formulation!!!!
            double objconst = isLagrangian? - jsonProblem.Edges.Select(edge => edge.Weight * LAMBDA).Sum() : 0;
            //setting the problem direction to minimization
            int objsens = WrapperCoin.SOLV_OBJSENS_MIN;
            double infinite = WrapperCoin.GetInfinity();
            //setting the lower bound to 0 -> all variables must be positive
            List<double> lowerBounds = Enumerable.Repeat(0.0, numberOfVariables).ToList();
            //setting the upper bound to infinity
            List<double> upperBounds = Enumerable.Repeat(infinite, numberOfVariables).ToList();
            /////////section of the coinmp initialization, read how to work with coinmp and the wrappercoinmp
            List<int> matrixBegin = Enumerable.Repeat(0, numberOfVariables + 1).ToList();
            List<int> matrixCount = Enumerable.Repeat(0, numberOfVariables).ToList();
            double[] n = Array.Empty<double>();
            char[] c = Array.Empty<char>();
            int[] i = Array.Empty<int>();
            //gets the objective coeffs from the flow object initalized before
            double[] objectCoeffs = flow.GetObjectiveCoeffs();
            List<Row> rows = flow.GetRows().ToList();
            WrapperCoin.LoadProblem(problem, numberOfVariables, 0, 0, 0, objsens, objconst, objectCoeffs, lowerBounds.ToArray(), upperBounds.ToArray(), c, n, null, matrixBegin.ToArray(), matrixCount.ToArray(), i, n
                , null, null, "");
            ////////////////////////
            //after the loading of the problem, i use the added CoinMP method AddRow to add all the constraints
            //calculated by the flow object
            rows.ForEach(x => WrapperCoin.AddRow(ref problem, x.Coeffs, x.ConstraintValue, x.ConstraintType, ""));
            //instructions used to set the type of the variables to Binary, telling they can only assume 0 or 1 values;
            //and telling to the coin solver to use the MILP algorithm.
            List<char> columnType = Enumerable.Repeat('B', numberOfVariables).ToList();
            WrapperCoin.LoadInteger(problem, columnType.ToArray());
        }

        //method used to generate the names of the variables to print it or to save the csv represntation of the problem
        private string CreateVariableNames() => string.Join(",", actualProblem.Commodities.SelectMany(commodity => (actualProblem.Edges.Select(edge =>
        {
            return commodity + "_" + edge.Source + "->" + edge.Destination;
        }))).Concat(isFirstFormulation ? _createXiName() : new ()).ToList().Select(x => $"{x}")).ToString();

        //it is used if using the first formulation and i need to give a name to the Xi variables.
        private List<string> _createXiName() => originalProblem.Commodities.SelectMany(commodity => actualProblem.Edges.Select(edge =>
        {
            return "Xi" + commodity + edge.Source + "->" + edge.Destination;
        })).ToList();
    }
}