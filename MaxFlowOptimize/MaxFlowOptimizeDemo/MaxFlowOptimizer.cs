using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WrapperCoinMP;

namespace MaxFlowOptimizeDemo
{
    public record Result(double objective, List<double> edgesValues);

    class MaxFlowOptimizer : IFlowOptimizer
    {
        private WrapProblem problem;

        public MaxFlowOptimizer(string problemName)
        {
            WrapperCoin.InitSolver();
            problem = WrapperCoin.CreateProblem(problemName);
        }


        record test(int edges, int vertexes, int links, List<double> objectiveCoeffs, List<double> lowerBoundsCoeffs, List<double> upperBoundsCoeffs, List<int> matrixBegin, List<int> matrixCount, List<int> matrixIndexes, List<double> matrixValues);

        public void ReadFromJSON(string path)
        {
            var serializer = new JsonSerializer();
            using StreamReader file = File.OpenText("../../../problem.json");
            using JsonTextReader reader = new JsonTextReader(file);
            var result = serializer.Deserialize<test>(reader);
            double[] objectiveCoeffs = ListToArray(result.objectiveCoeffs);
            double[] upperbounds = ListToArray(result.upperBoundsCoeffs);
            double[] lowerBoundsCoeffs = ListToArray(result.lowerBoundsCoeffs);
            double[] upperBoundsCoeffs = ListToArray(result.upperBoundsCoeffs);
            int[] matrixBegin = ListToArray(result.matrixBegin);
            int[] matrixCount = ListToArray(result.matrixCount);
            int[] matrixIndexes = ListToArray(result.matrixIndexes);
            double[] matrixValues = ListToArray(result.matrixValues);
            char[] rowType = CreateRepeatedArray('E',result.vertexes);
            double[] rhsRows = CreateRHS(result.vertexes);
            WrapperCoin.LoadProblem(problem, result.edges, result.vertexes, result.links, 0, WrapperCoin.SOLV_OBJSENS_MAX, 0, objectiveCoeffs, lowerBoundsCoeffs, upperBoundsCoeffs, rowType, rhsRows, null, matrixBegin, matrixCount, matrixIndexes, matrixValues
           , null, null, "");
            WrapperCoin.LoadInteger(problem, CreateRepeatedArray('I', result.vertexes));
        }
            

        public void addRow(List<double> values) => WrapperCoin.AddRow(ref problem, values.ToArray(), 0, 'E', "ff");
 

        public void nullifyRow(int row) => WrapperCoin.NullifyRow(problem, row);
        public Result OptimizeProblem()
        {
            WrapperCoin.OptimizeProblem(problem);

            double result = WrapperCoin.GetObjectValue(problem);
            double[] edgesV = new double[WrapperCoin.GetColCount(problem)];
            double[] reducedCost = new double[WrapperCoin.GetColCount(problem)];
            double[] slackV = new double[WrapperCoin.GetRowCount(problem)];
            double[] shadowPrice = new double[WrapperCoin.GetRowCount(problem)];

            WrapperCoin.GetSolutionValues(problem, edgesV, reducedCost, slackV, shadowPrice);
            return new Result(result, edgesV.ToList());
        }

        private char[] CreateRepeatedArray(char repeated, int rows) => Enumerable.Repeat(repeated, rows).ToArray();
        private double[] CreateRHS(int rows) => Enumerable.Repeat(0.0, rows).ToArray();


        private X[] ListToArray<X>(List<X> originList) => originList.ToArray();

        public void addVertex(int numberEdges, List<double> objectiveCoeff, List<double> lowerBounds, List<double> upperBounds, List<double> values)
        {
            List<int> edges = Enumerable.Range(0, numberEdges).ToList();
            edges.ForEach(x => WrapperCoin.AddColumn(problem, objectiveCoeff.ElementAt(x), upperBounds.ElementAt(x), lowerBounds.ElementAt(x)));
        }
    }
}
