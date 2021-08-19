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
    /// <summary>
    /// Interface that manages the way a flow problem is constructed. e.g. It can be created as
    /// a commodity flow problem or a net problem.
    /// It is intended to work with <see cref="WrapperCoinMP"/> 
    /// </summary>
    interface IFlowProblem
    {
        /// <summary>
        /// This method initializes the problem reading a graph from the provided json.
        /// </summary>
        /// <param name="loadedProblem"> The <see cref="JsonProblem"/> to read and initialize</param>
        void InizializeProblem(JsonProblem loadedProblem);
        /// <summary>
        /// This method returns the objective coeffs of the problem to initialize.
        /// </summary>
        /// <returns>An <see cref="Array"/> of <see cref="double"/> representing the objective function
        /// coeffs.</returns>
        double[] GetObjectiveCoeffs();
        /// <summary>
        /// This Method returns the rows of the problem to optimize.
        /// </summary>
        /// <returns>An <see cref="HashSet{T}"/> of <see cref="Row"/> with the rows of the problem.</returns>
        HashSet<Row> GetRows();
        /// <summary>
        /// Method that creates a <see cref="Result"/>.
        /// </summary>
        /// <param name="result">The optimal result.</param>
        /// <param name="optimizedValues">The optimal values for each variable.</param>
        /// <returns>The <see cref="Result"/> of the optimization.</returns>
        Result CreateResult(double result, List<double> optimizedValues);
    }
}
