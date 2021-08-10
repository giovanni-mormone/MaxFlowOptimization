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

			//s1(c0),s2(c1),21(c2),1d(c3),2d(c4)
			//s1<3,s2<5,1d<6,2d<4,21<1
			//max 2d+1d
			//c3-c0+c2=0
			//c4-c1=0
			IList<string> x = Enumerable.Range(0,5).Select(x => x.ToString()).ToList();
			x = x.Select((x,y) => x=="0"?x:"66").ToList();
			IFlowOptimizer flowOptimizer = new MaxFlowOptimizer("");
			flowOptimizer.ReadFromJSON("");
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			List<double> objective = new() {0,0}; 
			List<double> lower = new() {0,0}; 
			List<double> upper = new() {2,3};
			List<double> values = new() { 0, 0, 0, 0, 0, -1, 1 };
			List<double> values2 =  new(){ -1, 0, -1, 1, 0, 0, -1 };

			flowOptimizer.AddVertex(2, objective, lower, upper,values);
			flowOptimizer.AddRow(values);
			flowOptimizer.AddRow(values2);
			flowOptimizer.NullifyRow(0);
			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine($"Objective Result = {result.Objective}; EdgesValues:");
		//	result.EdgesValues.ForEach(x => Console.WriteLine($"{x}"));

			return 0;
		}
	}
}

