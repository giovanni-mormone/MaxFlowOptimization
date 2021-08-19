using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxFlowOptimizeDemo

{
	class Program
	{
		static int Main()
		{
			IFlowOptimizer flowOptimizer = new MaxFlowOptimizer("MaxFlow");
			flowOptimizer.ReadFromJSON("../../../problem.json");
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveToJSON("../../../modified.json");
			flowOptimizer.SaveMPS("loadedProblem");
			return 0;
		}
	}
}

