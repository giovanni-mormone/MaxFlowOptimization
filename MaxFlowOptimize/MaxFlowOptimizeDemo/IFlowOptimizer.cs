using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    interface IFlowOptimizer
    {
        void ReadFromJSON(string path);
        Result OptimizeProblem();
        void NullifyRow(int row);
        void AddRow(List<double> values);

        void AddVertex(int numberEdges, List<double> objectiveCoeff, List<double> lowerBounds, List<double> upperBounds, List<double> values);
    }
}
