using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    interface IFlowProblem
    {
        void InizializeProblem(JsonProblem loadedProblem);
        double[] GetObjectiveCoeffs();
        HashSet<Row> GetRows();
        Result CreateResult(double result, List<double> optimizedValues);
    }
}
