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
        void nullifyRow(int row);
        void addRow(List<double> values);

        void addVertex(int numberEdges, List<double> objectiveCoeff, List<double> lowerBounds, List<double> upperBounds, List<double> values);
    }
}
