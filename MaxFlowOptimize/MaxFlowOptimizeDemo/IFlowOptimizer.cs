using System.Collections.Generic;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;

namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// Interface used to manage and optimize a Flow problem. It works for a commodity flow or a multi commodity flow.
    /// </summary>
    interface IFlowOptimizer
    {
        
        /// <summary>
        /// Method used to read a Flow problem from the json file specified in the path parameter
        /// </summary>
        /// <param name="path"> a <see cref="string"/> used to represent the path of the json file from which the 
        /// problem is going to be read</param>
        void ReadFromJSON(string path);
        /// <summary>
        /// Method used to write the loaded Flow problem to the json file specified in the path parameter
        /// </summary>
        /// <param name="path"> a <see cref="string"/> used to represent the path where the json file 
        /// is going to be written</param>
        void SaveToJSON(string path);
        /// <summary>
        /// Method used to write the result of the optimized problem to the json file specified in the path parameter.
        /// </summary>
        /// <param name="path"> a <see cref="string"/> used to represent the path where the json file 
        /// is going to be written</param>
        void SaveResult(string path);
        /// <summary>
        /// Method used to write the loaded problem to the MPS file specified in the path parameter.
        /// </summary>
        /// <param name="path"> a <see cref="string"/> used to represent the path where the MPS file 
        /// is going to be written</param>
        void SaveMPS(string fileNamePath);
        /// <summary>
        /// Method used to optimize the loaded flow problem
        /// </summary>
        /// <returns> A <see cref="Result"/> representing the result of the optimization of the problem</returns>
        Result OptimizeProblem();
        /// <summary>
        /// Method used to add a new edge to the problem
        /// </summary>
        /// <param name="Edge">The <see cref="Edge"/> to be added to the problem. It only adds an edge if the 
        /// source and destination node of the edge to be added are present in the problem, i.e. if the source is either a source or a node
        /// and the destination is either a sink or a node. If an edge is already present between source and destination it does nothing</param>
        void AddEdge(Edge Edge);
        /// <summary>
        /// Method used to update an edge of the problem
        /// </summary>
        /// <param name="Edge">The <see cref="Edge"/> to be updated to the problem. It only updates an edge if the 
        /// the edge is present in the problem. If an edge is not already present it does nothing</param>
        void UpdateEdge(Edge Edge);
        /// <summary>
        /// Method used to remove an edge from the problem
        /// </summary>
        /// <param name="Edge">The <see cref="Edge"/> to be removed from the problem. If the edge does not exist, it does
        /// nothing
        void RemoveEdge(Edge Edge);
        /// <summary>
        /// Method used to add a new node to the problem.
        /// </summary>
        /// <param name="Name">The <see cref="string"/> name of the node to be added. If a node with the same name already
        /// exists, it does nothing</param>
        /// <param name="Edges">The <see cref="HashSet{Edge}"/> representing the edges of the node to be added.
        void AddNode(string Name, HashSet<Edge> Edges);
        /// <summary>
        /// Method used to remove a node from the problem. It also removes from the problem all the edges where the node is
        /// either source or destination.
        /// </summary>
        /// <param name="Name">The <see cref="string"/> name of the node to remove.</param>
        void RemoveNode(string Name);
        /// <summary>
        /// Method used to add a source node to the problem. It only adds the source if it is not present and
        /// at least one of the commodities passed as input is present in the problem.
        /// </summary>
        /// <param name="Source"> The <see cref="SinkSource"/> to add to the problem. If the source is already present
        /// it does nothing.</param>
        /// <param name="Commodities">The <see cref="List{string}"/> representing of which commodity the added source
        /// is a source. If none of the commodities passed is present in the problem, the source is not added.</param>
        void AddSource(SinkSource Source, List<string> Commodities);
        /// <summary>
        /// Method used to remove a source from the problem. It also removes the sorce from the commodity sources
        /// and removes every edge involving the source.
        /// </summary>
        /// <param name="Source">The <see cref="string"/> representing the source to remove. If there is no source
        /// with the given name, it does nothing.</param>
        void RemoveSource(string Source);
        /// <summary>
        /// Method used to add a sink node to the problem. It only adds the sink if it is not present and
        /// at least one of the commodities passed as input is present in the problem.
        /// </summary>
        /// <param name="Sink"> The <see cref="SinkSource"/> to add to the problem. If the sink is already present
        /// it does nothing.</param>
        /// <param name="Commodities">The <see cref="List{string}"/> representing of which commodity the added sink
        /// accepts. If none of the commodities passed is present in the problem, the sink is not added.</param>
        void AddSink(SinkSource Sink, List<string> Commodities);
        /// <summary>
        /// Method used to remove a sink from the problem. It also removes the sink from the commodity sinks
        /// and removes every edge involving the sink.
        /// </summary>
        /// <param name="Sink">The <see cref="string"/> representing the sink to remove. If there is no sink
        /// with the given name, it does nothing.</param>
        void RemoveSink(string Sink);
        /// <summary>
        /// Method used to add a new commodity to the problem. If already present it does nothing.
        /// </summary>
        /// <param name="Commodity">The <see cref="string"/> representing the commodity to add.</param>
        void AddCommodity(string Commodity);
        /// <summary>
        /// Method used to remove a commodity from the problem. If the commodity is not present it does nothing.
        /// It also removes all the commody sinks and source for the given commodity.
        /// If after the removal of a commmodity a source or a sink does not have any other associated commodity, it is
        /// removed from the problem.
        /// </summary>
        /// <param name="Commodity">The <see cref="string"/> representing the commodity to remove.</param>
        void RemoveCommodity(string Commodity);
        /// <summary>
        /// Method used to add a commodity to a source. It only does something if the source and the commodity are present in the problem.
        /// </summary>
        /// <param name="Source">The <see cref="string"/> the name of the source to which the commodity is going to be added.</param>
        /// <param name="Commodity"> The <see cref="string"/> commodity that is going to be added to the given source.</param>
        void AddCommodityToSource(string Source, string Commodity);
        /// <summary>
        /// Method used to remove a commodity from a source. If the commodity removed is the last, the source is removed too.
        /// </summary>
        /// <param name="Source">The <see cref="string"/> the name of the source from which the commodity is going to be removed.</param>
        /// <param name="Commodity"> The <see cref="string"/> commodity that is going to be remove from the given source.</param>
        void RemoveCommodityFromSource(string Source, string Commodity);
        /// <summary>
        /// Method used to add a commodity to a sink. It only does something if the sink and the commodity are present in the problem.
        /// </summary>
        /// <param name="Sink">The <see cref="string"/> the name of the sink to which the commodity is going to be added.</param>
        /// <param name="Commodity"> The <see cref="string"/> commodity that is going to be added to the given sink.</param>
        void AddCommodityToSink(string Sink, string Commodity);
        /// <summary>
        /// Method used to remove a commodity from a sink. If the commodity removed is the last, the sink is removed too.
        /// </summary>
        /// <param name="Sink">The <see cref="string"/> the name of the sink from which the commodity is going to be removed.</param>
        /// <param name="Commodity"> The <see cref="string"/> commodity that is going to be remove from the given sink.</param>
        void RemoveCommodityFromSink(string Sink, string Commodity);

        void PrintProblemRows();

    }
}
