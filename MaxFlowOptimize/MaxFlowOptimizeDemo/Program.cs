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
			flowOptimizer.ReadFromJSON("../../../Construct.json");
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.AddCommodity("k11");
			flowOptimizer.AddSource(new SinkSource("s", 15), new List<string> { "k11" }); 
			flowOptimizer.AddSink(new SinkSource("t", 10), new List<string> { "k11" });
			flowOptimizer.AddNode("n2", new HashSet<Edge> { new Edge(-1, "s", "n2"), new Edge(-1, "n2", "t"), new Edge(-1, "n2", "t1"), new Edge(-1,"s1","n2") });
			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.RemoveCommodity("k1");
			flowOptimizer.AddCommodityToSource("s1","k11");

			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveToJSON("../../../ahahah.json");
			return 0;
		}
	}
}

