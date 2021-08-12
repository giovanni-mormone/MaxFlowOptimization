using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    public class Edge
    {
        public readonly int Weigth;
        public readonly string Source;
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
