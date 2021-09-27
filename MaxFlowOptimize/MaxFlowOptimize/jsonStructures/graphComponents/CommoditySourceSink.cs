using System;

namespace MaxFlowOptimizeDemo.jsonStructures.graphComponents

{
    public class CommoditySourceSink
    {
        /// <summary>
        ///The<see cref="System.Double"/> representing  the capacity of a Source/Sink </summary>
        public readonly double Weight;
        /// <summary>
        /// The name of the source/sink.
        /// </summary>
        public readonly string Name;
        /// <summary>
        ///The name of the commodity.</param>
        /// </summary>
        public readonly string Commodity;

        public CommoditySourceSink(string Name, string Commodity, double Weight = -1)
        {
            this.Weight = Weight;
            this.Name = Name;
            this.Commodity = Commodity;
        }

        public override bool Equals(object obj) => obj is CommoditySourceSink sourceSink &&
            Name == sourceSink.Name &&
            Commodity == sourceSink.Commodity;

        public override int GetHashCode() => HashCode.Combine(Name, Commodity);
    }
}
