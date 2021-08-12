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
        void SaveToJSON(string path);
        Result OptimizeProblem();
        void AddEdge(Edge Edge);
        void RemoveEdge(Edge Edge);
        void AddNode(string Name, HashSet<Edge> Edges);
        void RemoveNode(string Name);
        void AddSource(SinkSource Sink, List<string> Commodities);
        void RemoveSource(string Source);
        void AddSink(SinkSource Sink, List<string> Commodities);
        void RemoveSink(string Source);
        void AddCommodity(string Commodity);
        void RemoveCommodity(string Commodity);
        void AddCommodityToSource(string Source, string Commodity);
        void RemoveCommodityFromSource(string Source, string Commodity);

    }
}
