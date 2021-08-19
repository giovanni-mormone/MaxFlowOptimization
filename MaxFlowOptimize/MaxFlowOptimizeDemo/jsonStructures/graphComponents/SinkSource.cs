namespace MaxFlowOptimizeDemo.jsonStructures.graphComponents

{
    /// <summary>
    /// The record used to represent a pair of Source/Sink and its capacity.
    /// </summary>
    /// <param name="Name">The <see cref="System.String"/> representing the name of the Source/Sink.</param>
    /// <param name="Capacity">The <see cref="System.Double"/> representing  the capacity of a Source/Sink. If
    /// -1 it represents the infinite, or a Source/Sink with no limits.</param>
    public record SinkSource(string Name, double Capacity);

}
