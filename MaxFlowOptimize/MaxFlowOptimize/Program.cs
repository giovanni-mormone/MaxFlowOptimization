using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using System;
using System.IO;
using Newtonsoft.Json;

namespace MaxFlowOptimizeDemo

{
	class Program
	{
		// record of config parameters
		public record LoadingParameters (bool isVerbose, string problemFile, int nmax);

		static void Main()
		{
			LoadingParameters parConfig = readConfig();
			RunOptimiziation(parConfig.problemFile, parConfig.nmax, parConfig.isVerbose);
		}

		private static void RunOptimiziation(string problemName, int edgeMultiplier, bool isVerbose)
      {
			IFlowOptimizer flowOptimizer = new FlowOptimizer("MaxFlow", new FlowProblemFormulation(edgeMultiplier), true);
			flowOptimizer.ReadFromJSON($"../../../Resources/{problemName}");
			Console.WriteLine("Problema Caricato:");
			if (isVerbose)
				flowOptimizer.PrintProblemRows();
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
//			flowOptimizer.SaveMPS($"../../../Resources/loadedProblem-{problemName}");
			flowOptimizer.SaveCSV($"../../../Resources/loadedProblem-{problemName}");
			//			flowOptimizer.SaveResult($"../../../Resources/problemResult-{problemName}");
			flowOptimizer.LagrangianOptimization();
			if (isVerbose)
				flowOptimizer.PrintProblemRows();
			result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveCSV($"../../../Resources/loadedProblemLagrangianVersion-{problemName}");

		}



		static LoadingParameters readConfig()
		{
			{
				string path = "";
				StreamReader fileConfig = null;
				LoadingParameters p = null;
				try
				{
					string confPath = File.Exists("MaxFlowConfig.json") ? "MaxFlowConfig.json" : @"..\..\..\MaxFlowConfig.json";
					fileConfig = new StreamReader(confPath);
					string jConfig = fileConfig.ReadToEnd();
					p = JsonConvert.DeserializeObject<LoadingParameters>(jConfig);
				}
				catch (Exception ex)
				{
					Console.WriteLine("[CONFIG ERROR] " + ex.Message);
				}
				finally
				{
					if (fileConfig != null)
						fileConfig.Close();
				}
				Console.WriteLine("Loading config from " + path);
				return p;
			}
		}
	}
}

