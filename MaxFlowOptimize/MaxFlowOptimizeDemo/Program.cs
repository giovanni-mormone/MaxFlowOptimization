using MaxFlowOptimizeDemo.result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxFlowOptimizeDemo

{
	class Program
	{
		static int Main()
		{
			IFlowOptimizer flowOptimizer = new FlowOptimizer("MaxFlow", new FlowProblem());
			flowOptimizer.ReadFromJSON("../../../Resources/problem.json");
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveToJSON("../../../Resources/modified.json");
			flowOptimizer.SaveMPS("../../../Resources/loadedProblem");
			return 0;
		}
	}
}

