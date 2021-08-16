namespace MaxFlowOptimizeDemo
{
    /// <summary>
    /// Record used to represent a commodity, read from a json file.
    /// </summary>
    /// <param name="CommodityName">The name of the commodity.</param>
    /// <param name="CommodityNumber"> The number assigned to the commodity.</param>
    public record Commodity(string CommodityName, int CommodityNumber);
}
