using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// A Class used to Represent an edge of the flow problem to be resolved.
    /// To not duplicate edges, each edge is equal to another if the source and the destination are the same:
    /// there can not be two edges from the same source and destination but with different weigths.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// The Weigth of the edge
        /// </summary>
        public readonly int Weigth;
        /// <summary>
        /// The  Source node of the edge
        /// </summary>
        public readonly string Source;
        /// <summary>
        /// The destination node of the edge
        /// </summary>
        public readonly string Destination;

        public Edge(int Weigth,string Source,string Destination)
        {
            this.Weigth = Weigth;
            this.Source = Source;
            this.Destination = Destination;
        }

        public override bool Equals(object obj) => obj is Edge edge &&
            Source == edge.Source &&
            Destination == edge.Destination;

        public override int GetHashCode() => HashCode.Combine(Source, Destination);

    }
}
