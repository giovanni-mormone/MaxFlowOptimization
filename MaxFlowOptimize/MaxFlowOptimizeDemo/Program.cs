using System;
using System.Collections.Generic;
using System.Linq;
using WrapperCoinMP;

namespace MaxFlowOptimizeDemo

{
	class Program
	{
		static int Main()
		{
			
			IList<string> x = Enumerable.Range(0,5).Select(x => x.ToString()).ToList();
			x = x.Select((x,y) => x=="0"?x:"66").ToList();
			IFlowOptimizer flowOptimizer = new MaxFlowOptimizer("");
			flowOptimizer.ReadFromJSON("../../../problem - Copia.json");
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			List<Edge> xxx = new List<Edge>();
			xxx.Add(new Edge(2, "s1", "caca"));
			xxx.Add(new Edge(2, "s2", "caca"));
			xxx.Add(new Edge(-1, "caca", "t1"));
			xxx.Add(new Edge(-1, "caca", "t2"));

			flowOptimizer.AddNode("caca", new Edges(4, xxx));
			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			return 0;
		}
	}
}

