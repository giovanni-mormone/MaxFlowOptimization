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
		public record parms (
			bool isVerbose,
			string problemFile,
			int nmax
		);

		static int Main()
		{
			parms parConfig = readConfig();
			RunOptimiziation(parConfig.problemFile, 
			                 parConfig.nmax,
								  parConfig.isVerbose);
			return 0;
		}

		private static void RunOptimiziation(string problemName, int edgeMultiplier, bool isVerbose)
      {
			IFlowOptimizer flowOptimizer = new FlowOptimizer("MaxFlow", new FlowProblemFormulationAlt(edgeMultiplier),true);
			flowOptimizer.ReadFromJSON($"../../../Resources/{problemName}");
			Console.WriteLine("Problema Caricato:");
			 if(isVerbose)
				flowOptimizer.PrintProblemRows();
			Result result = flowOptimizer.OptimizeProblem();
			Console.WriteLine(result);
			flowOptimizer.SaveMPS($"../../../Resources/loadedProblem-{problemName}");
			flowOptimizer.SaveCSV($"../../../Resources/loadedProblem-{problemName}");
			flowOptimizer.SaveResult($"../../../Resources/problemResult-{problemName}");
		}

		static parms readConfig()
		{
			{
				string path = "";
				StreamReader fileConfig = null;
				parms p = null;
				try
				{
					string confPath = File.Exists("MaxFlowConfig.json") ? "MaxFlowConfig.json" : @"..\..\..\MaxFlowConfig.json";
					fileConfig = new StreamReader(confPath);
					string jConfig = fileConfig.ReadToEnd();
					p = JsonConvert.DeserializeObject<parms>(jConfig);
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

