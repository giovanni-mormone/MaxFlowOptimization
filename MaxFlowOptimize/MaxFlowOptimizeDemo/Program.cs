using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using System;

namespace MaxFlowOptimizeDemo

{
	class Program
	{
		static int Main()
		{
			RunOptimiziation("problem", 2);
			RunOptimiziation("problem2", 2);
			return 0;
		}

		private static void RunOptimiziation(string problemName, int edgeMultiplier)
        {
			IFlowOptimizer flowOptimizer = new FlowOptimizer("MaxFlow", new NewFormulationFlowProblem(edgeMultiplier));
			flowOptimizer.ReadFromJSON($"../../../Resources/{problemName}.json");
			Console.WriteLine("Problema Caricato:");
			flowOptimizer.PrintProblemRows();
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveMPS($"../../../Resources/loadedProblem-{problemName}");
			flowOptimizer.SaveResult($"../../../Resources/problemResult-{problemName}.json");
			flowOptimizer.UpdateEdge(new Edge(5, "n1", "n2"));
			flowOptimizer.UpdateEdge(new Edge(7, "n1", "n4"));
			flowOptimizer.UpdateEdge(new Edge(16, "n3", "n2"));
			flowOptimizer.UpdateEdge(new Edge(11, "n3", "n4"));
			Console.WriteLine("Problema Modificato:");
			flowOptimizer.PrintProblemRows();
			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveMPS($"../../../Resources/updatedProblem-{problemName}");
			flowOptimizer.SaveResult($"../../../Resources/updatedResult-{problemName}.json");
		}
	}
}

