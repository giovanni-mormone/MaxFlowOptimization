namespace MaxFlowOptimizeDemo.jsonStructures.graphComponents

{
    /// <summary>
    /// The record used to model a row of the problem. It is based on the sintax needed by the <see cref="WrapperCoinMP.WrapperCoin"/> add row method.
    /// </summary>
    /// <param name="Coeffs"> The <see cref="System.Array"/> of <see cref="System.Double"/> representing the coeffs values of the row.</param>
    /// <param name="ConstraintValue">The <see cref="System.Double"/> value of the constraint of the row; it represents the RHS of a row.</param>
    /// <param name="ConstraintType">The <see cref="System.Char"/> representing the type of the constraint.</param>
    public record Row(double[] Coeffs, double ConstraintValue, char ConstraintType);
}
