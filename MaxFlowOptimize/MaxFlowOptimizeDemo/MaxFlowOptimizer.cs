﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void AddNode(string Name, Edges Edges)
        {
            problem = WrapperCoin.CreateProblem(WrapperCoin.GetProblemName(problem));
            actualProblem = new JsonProblem(actualProblem.Sources, actualProblem.Sinks,
                new Pair(actualProblem.Nodes.Number + 1, actualProblem.Nodes.Names.Append(Name).ToList()),
                actualProblem.Commodities, actualProblem.CommoditiesSources, actualProblem.Edges.AddEdges(Edges));
            InitializeWrapperProblem(actualProblem);
        }

        public void AddRow(List<double> values) => WrapperCoin.AddRow(ref problem, values.ToArray(), 0, 'E', "ff");
 

        public void NullifyRow(int row) => WrapperCoin.NullifyRow(problem, row);
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

        public void AddVertex(int numberEdges, List<double> objectiveCoeff, List<double> lowerBounds, List<double> upperBounds, List<double> values)
        {
            List<int> edges = Enumerable.Range(0, numberEdges).ToList();
            edges.ForEach(x => WrapperCoin.AddColumn(problem, objectiveCoeff.ElementAt(x), upperBounds.ElementAt(x), lowerBounds.ElementAt(x)));
        }

        private void InitializeWrapperProblem(JsonProblem jsonProblem)
        {
            int numberOfVariables = jsonProblem.Edges.EdgesNumber * jsonProblem.Commodities.Number;
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
