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
            double[] r = ListToArray(result.upperBoundsCoeffs);
            //WrapperCoin.LoadProblem(problem, result.edges, result.vertexes, result.links, nrng, objsens, objconst, objectCoeffs, lowerBounds, upperBounds, rowType, drhs, null, mbeg, mcnt, midx, mval
           //, colNames, rowNames, objectname);
        }

        private X[] ListToArray<X>(List<X> originList) => originList.ToArray();
    }
}
