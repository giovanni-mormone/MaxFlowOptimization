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
        /// This method initializes the problem reading a graph from the provided json. It uses the first formulation of 
        /// the problem, with the commodities duplicated in couples soruce/sink; the paramater commodityGroups is needed
        /// to keep track of the association between true commodities and the new created when modifying the problem.
        /// </summary>
        /// <param name="loadedProblem"> The <see cref="JsonProblem"/> to read and initialize</param>
        /// <param name="commodityGroups"> The <see cref="Dictionary{TKey, TValue}"/> used to represent the pairs of true commodity
        /// adn generated source/destination commodities</param>
        void InizializeProblem(JsonProblem loadedProblem, Dictionary<String, List<String>> commodityGroups);

        /// <summary>
        /// Method used to initialize a problem in lagrangian form, with an objective function with modified costs for the
        /// Xi variables.
        /// </summary>
        /// <param name="loadedProblem"> The <see cref="JsonProblem"/> to read and initialize</param>
        /// <param name="commodityGroups"> The <see cref="Dictionary{TKey, TValue}"/> used to represent the pairs of true commodity
        void CreateLagrangian(JsonProblem loadedProblem, Dictionary<string, List<string>> commodityGroups, int lambda);

        /// <summary>
        /// This method initializes the problem reading a graph from the provided json and using an alternative formulation
        /// </summary>
        /// <param name="loadedProblem"> The <see cref="JsonProblem"/> to read and initialize</param>
        void InizializeProblemAlternativeFormulation(JsonProblem loadedProblem);
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
        /// Method used to create a <see cref="Result"/> from the given parameters
        /// </summary>
        /// <param name="result"> The <see cref="double"/> result.</param>
        /// <param name="optimizedValues"> The <see cref="List{double}"/> optimized values for each edge.</param>
        /// <returns></returns>
        Result CreateResult(double result, List<double> optimizedValues);

        /// <summary>
        /// Method used to indicate the edges that must be associated with a penality. It takes an edge and adds it
        /// to a collection of penality edges.
        /// </summary>
        /// <param name="edge">The <see cref="Edge"/> that has a penality associated to it</param>
        void AddPenality(Edge edge);
    }
}