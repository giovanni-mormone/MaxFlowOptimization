using System;
using System.Collections.Generic;
using WrapperCoinMP;

namespace MaxFlowOptimizeDemo

{
	class Program
	{
		static int Main(string[] args)
		{

			//s1(c0),s2(c1),21(c2),1d(c3),2d(c4)
			//s1<3,s2<5,1d<6,2d<4,21<1
			//max 2d+1d
			//c3-c0+c2=0
			//c4-c1=0
			IFlowOptimizer flowOptimizer = new MaxFlowOptimizer("");
			flowOptimizer.ReadFromJSON("");
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine($"Objective Result = {result.objective}; EdgesValues:");
			result.edgesValues.ForEach(x => Console.WriteLine($"{x}"));
			List<double> objective = new() {0,0}; 
			List<double> lower = new() {0,0}; 
			List<double> upper = new() {2,3};
			List<double> values = new() { 0, 0, 0, 0, 0, -1, 1 };
			List<double> values2 =  new(){ -1, 0, -1, 1, 0, 0, -1 };

			flowOptimizer.addVertex(2, objective, lower, upper,values);
			flowOptimizer.addRow(values);
			flowOptimizer.addRow(values2);
			flowOptimizer.nullifyRow(0);
			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine($"Objective Result = {result.objective}; EdgesValues:");
			result.edgesValues.ForEach(x => Console.WriteLine($"{x}"));

			return 0;
		}
	}
}

