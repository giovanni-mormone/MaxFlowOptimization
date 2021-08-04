using System;
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
			IFlowOptimizer f = new MaxFlowOptimizer("a");
			f.ReadFromJSON("a");
			const int NUM_COLS = 5;
			const int NUM_ROWS = 2;
			const int NUM_NZ = 6;
			const int NUM_COLS2 = 7;
			const int NUM_ROWS2 = 4;
			const int NUM_NZ2 = 12;
			const int NUM_RNG = 0;
			string probname = "MaxFlow";

			/*
			string probname = "Afiro";*/
			int ncol = NUM_COLS;
			int nrow = NUM_ROWS;
			int nels = NUM_NZ;
			int ncol2 = NUM_COLS2;
			int nrow2 = NUM_ROWS2;
			int nels2 = NUM_NZ2;
			int nrng = NUM_RNG;

			string objectname = "Cost";
			int objsens = WrapperCoin.SOLV_OBJSENS_MAX;
			double objconst = 0.0;
			double infinite = WrapperCoin.GetInfinity();
			double[] objectCoeffs = new double[NUM_COLS] { 0, 0, 0, 1, 1 };
			double[] lowerBounds = new double[NUM_COLS] { 0, 0, 0, 0, 0 };
			double[] upperBounds = new double[NUM_COLS] { 3, 5, 1, 6, 4 };
			double[] objectCoeffs2 = new double[NUM_COLS2] { 0, 0, 0, 1, 1, 0, 0 };
			double[] lowerBounds2 = new double[NUM_COLS2] { 0, 0, 0, 0, 0, 0, 0 };
			double[] upperBounds2 = new double[NUM_COLS2] { 3, 5, 1, 6, 4, 2, 3 };

			char[] rowType = new char[NUM_ROWS] { 'E', 'E' };
			char[] rowType2 = new char[NUM_ROWS2] { 'E', 'E', 'E', 'E' };
			double[] drhs = new double[NUM_ROWS] { 0, 0 };// 
			double[] drhs2 = new double[NUM_ROWS2] { 0, 0, 0, 0 };// 
			int[] mbeg = new int[NUM_COLS + 1] { 0, 1, 2, 4, 5, 6 };
			int[] mbeg2 = new int[NUM_COLS2 + 1] { 0, 2, 3, 6, 8, 9, 10, 12 };

			int[] mcnt = new int[NUM_COLS] { 1, 1, 2, 1, 1 };
			int[] mcnt2 = new int[NUM_COLS2] { 2, 1, 3, 2, 1, 1, 2 };

			//s1(c0),s2(c1),21(c2),1d(c3),2d(c4),s3(c5), 31(c6)
			//s1<3,s2<5,1d<6,2d<4,21<1
			//max 2d+1d
			//-c0-c2+c3=0
			//-c1+c2+c4=0
			//-c5+c6 = 0
			//-c0-c2+c3-c6=0

			int[] midx = new int[NUM_NZ] { 0, 1, 0, 1, 0, 1 };
			double[] mval = new double[NUM_NZ] { -1, -1, -1, 1, 1, 1 };
			int[] midx2 = new int[NUM_NZ2] { 0, 3, 1, 0, 1, 3, 0, 3, 1, 2, 2, 3 };
			double[] mval2 = new double[NUM_NZ2] { -1, -1, -1, -1, 1, -1, 1, 1, 1, -1, 1, -1 };
			string[] colNames = new string[NUM_COLS] { "c0", "c1", "c2", "c3", "c4" };
			string[] colNames2 = new string[NUM_COLS2] { "c0", "c1", "c2", "c3", "c4", "c5", "c6" };
			string[] rowNames = new string[NUM_ROWS] { "r1", "r2" };
			string[] rowNames2 = new string[NUM_ROWS2] { "r1", "r2", "r3", "r4" };
			WrapProblem problem = WrapperCoin.CreateProblem(probname);
			WrapProblem problem2 = WrapperCoin.CreateProblem(probname);
			WrapperCoin.LoadProblem(problem, ncol, nrow, nels, nrng, objsens, objconst, objectCoeffs, lowerBounds, upperBounds, rowType, drhs, null, mbeg, mcnt, midx, mval
				, colNames, rowNames, objectname);
			WrapperCoin.LoadProblem(problem2, ncol2, nrow2, nels2, nrng, objsens, objconst, objectCoeffs2, lowerBounds2, upperBounds2, rowType2, drhs2, null, mbeg2, mcnt2, midx2, mval2
				, colNames2, rowNames2, objectname);

			char[] integers = new char[NUM_COLS] { 'I', 'I', 'I', 'I', 'I' };
			char[] integers2 = new char[NUM_COLS2] { 'I', 'I', 'I', 'I', 'I', 'I', 'I' };

			WrapperCoin.LoadInteger(problem, integers);
			WrapperCoin.LoadInteger(problem2, integers2);
			WrapperCoin.OptimizeProblem(problem);
			WrapperCoin.OptimizeProblem(problem2);
			double res = WrapperCoin.GetObjectValue(problem);
			double res2 = WrapperCoin.GetObjectValue(problem2);
			double[] activity = new double[NUM_COLS2];
			double[] reducedCost = new double[NUM_COLS2];
			double[] slackValues = new double[NUM_ROWS2];
			double[] shadowPrice = new double[NUM_ROWS2];
			double[] activity2 = new double[NUM_COLS2];
			double[] reducedCost2 = new double[NUM_COLS2];
			double[] slackValues2 = new double[NUM_ROWS2];
			double[] shadowPrice2 = new double[NUM_ROWS2];
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			WrapperCoin.GetSolutionValues(problem2, activity2, reducedCost2, slackValues2, shadowPrice2);
			Console.WriteLine("Prob1");
			printResult(res, activity);
			Console.WriteLine("Prob2");
			printResult(res2, activity2);
			WrapperCoin.NullifyRow(problem2, 0);

			WrapperCoin.OptimizeProblem(problem2);
			res2 = WrapperCoin.GetObjectValue(problem2);
			WrapperCoin.GetSolutionValues(problem2, activity2, reducedCost2, slackValues2, shadowPrice2);

			Console.WriteLine("Prob2");
			printResult(res2, activity2);
			WrapperCoin.AddColumn(problem, 0, 2, 0);
			int cc = WrapperCoin.GetColCount(problem);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);

			WrapperCoin.AddColumn(problem, 0, 3, 0);
			cc = WrapperCoin.GetColCount(problem);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);

			WrapperCoin.AddRow(ref problem, new double[] { 0, 0, 0, 0, 0, -1, 1 }, 0, 'E', "ff");
			//problem.setProblem(p);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);

			WrapperCoin.OptimizeProblem(problem);
			res = WrapperCoin.GetObjectValue(problem);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			WrapperCoin.AddRow(ref problem, new double[] { -1, 0, -1, 1, 0, 0, -1 }, 0, 'E', "FF");
			//problem.setProblem(p1);
			WrapperCoin.OptimizeProblem(problem);
			res = WrapperCoin.GetObjectValue(problem);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			WrapperCoin.NullifyRow(problem, 0);
			WrapperCoin.OptimizeProblem(problem);
			res = WrapperCoin.GetObjectValue(problem);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			Console.WriteLine("Prob1");
			printResult(res, activity);
			/*
			int[] a = new int[] { 0, 1 };
			int[] b = new int[] { 0, 1 };
			double[] c = new double[] { 0, -1.0 };
			double[] d = new double[] { 2.5, 0 };
			char[] e = new char[4] { 'L','F','G','L' };
			string ss;
			ss = WrapperCoin.GetProblemName(problem);
			string boh = WrapperCoin.GetRowName(problem, 0);
			WrapperCoin.OptimizeProblem(problem);

			double res = WrapperCoin.GetObjectValue(problem);
			string na = "cc";
			IntPtr p = WrapperCoin.AddRow(problem, c, 3, 'L',na);
			problem.setProblem(p);
			problems = WrapperCoin.CheckProblem(problem);
			//boh = WrapperCoin.GetRowName(problem, 0); 
			cc = WrapperCoin.GetColCount(problem);
			ss = WrapperCoin.GetProblemName(problem);
			int cc2 = WrapperCoin.GetRowCount(problem);
			WrapperCoin.OptimizeProblem(problem);
			/*string a = WrapperCoin.GetVersionStr();
			Console.WriteLine("SIAMO SU LINUX ED E VERSIONE: " + a);
			const int NUM_COLS = 32;
			const int NUM_ROWS = 27;
			const int NUM_NZ = 83;
			const int NUM_RNG = 0;
			const double DBL_MAX = 1e+037;

			string probname = "Afiro";
			int ncol = NUM_COLS;
			int nrow = NUM_ROWS;
			int nels = NUM_NZ;
			int nrng = NUM_RNG;

			string objectname = "Cost";
			int objsens = WrapperCoin.SOLV_OBJSENS_MIN;
			double objconst = 0.0;

			double[] dobj = new double[NUM_COLS] {0, -0.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.32, 0, 0, 0, -0.6,
												  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.48, 0, 0, 10};

			double[] dclo = new double[NUM_COLS] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
												  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

			double[] dcup = new double[NUM_COLS] {DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX,
								  DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX,
								  DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX,
								  DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX, DBL_MAX};

			char[] rtyp = new char[NUM_ROWS] {'E', 'E', 'L', 'L', 'E', 'E', 'L', 'L', 'L', 'L', 'E', 'E', 'L',
										 'L', 'E', 'E', 'L', 'L', 'L', 'L', 'L', 'L', 'L', 'L', 'L', 'L', 'L'};

			double[] drhs = new double[NUM_ROWS] {0, 0, 80, 0, 0, 0, 80, 0, 0, 0, 0, 0, 500, 0, 0, 44, 500, 0,
												  0, 0, 0, 0, 0, 0, 0, 310, 300};

			int[] mbeg = new int[NUM_COLS + 1] {0, 4, 6, 8, 10, 14, 18, 22, 26, 28, 30, 32, 34, 36, 38, 40,
									  44, 46, 48, 50, 52, 56, 60, 64, 68, 70, 72, 74, 76, 78, 80, 82, 83};

			int[] mcnt = new int[NUM_COLS] {4, 2, 2, 2, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2, 4,
											4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 1};

			int[] midx = new int[NUM_NZ] {0, 1, 2, 23, 0, 3, 0, 21, 1, 25, 4, 5, 6, 24, 4, 5, 7, 24, 4, 5,
							   8, 24, 4, 5, 9, 24, 6, 20, 7, 20, 8, 20, 9, 20, 3, 4, 4, 22, 5, 26, 10, 11,
							   12, 21, 10, 13, 10, 23, 10, 20, 11, 25, 14, 15, 16, 22, 14, 15, 17, 22, 14,
							   15, 18, 22, 14, 15, 19, 22, 16, 20, 17, 20, 18, 20, 19, 20, 13, 15, 15, 24,
							   14, 26, 15};

			double[] mval = new double[NUM_NZ] {-1, -1.06, 1, 0.301, 1, -1, 1, -1, 1, 1, -1, -1.06, 1, 0.301,
							  -1, -1.06, 1, 0.313, -1, -0.96, 1, 0.313, -1, -0.86, 1, 0.326, -1, 2.364, -1,
							  2.386, -1, 2.408, -1, 2.429, 1.4, 1, 1, -1, 1, 1, -1, -0.43, 1, 0.109, 1, -1,
							  1, -1, 1, -1, 1, 1, -0.43, 1, 1, 0.109, -0.43, 1, 1, 0.108, -0.39, 1, 1,
							  0.108, -0.37, 1, 1, 0.107, -1, 2.191, -1, 2.219, -1, 2.249, -1, 2.279, 1.4,
							  -1, 1, -1, 1, 1, 1};

			string[] colNames = new string[NUM_COLS] {"x01", "x02", "x03", "x04", "x06", "x07", "x08", "x09",
								 "x10", "x11", "x12", "x13", "x14", "x15", "x16", "x22", "x23", "x24", "x25",
								 "x26", "x28", "x29", "x30", "x31", "x32", "x33", "x34", "x35", "x36", "x37",
								 "x38", "x39"};

			string[] rowNames = new string[NUM_ROWS] {"r09", "r10", "x05", "x21", "r12", "r13", "x17", "x18",
								 "x19", "x20", "r19", "r20", "x27", "x44", "r22", "r23", "x40", "x41", "x42",
								 "x43", "x45", "x46", "x47", "x48", "x49", "x50", "x51"};

			WrapProblem problem = WrapperCoin.CreateProblem(probname);
			WrapperCoin.LoadProblem(problem, ncol, nrow, nels, nrng, objsens, objconst, dobj, dclo, dcup, rtyp, drhs, null, mbeg, mcnt, midx, mval
				, colNames, rowNames, objectname);
			WrapperCoin.OptimizeProblem(problem);
			double res = WrapperCoin.GetObjectValue(problem);
			double[] activity = new double[ncol];
			double[] reducedCost = new double[nrow];
			double[] slackValues = new double[ncol];
			double[] shadowPrice = new double[nrow];
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			double optimalValue = -464.7531428571;
			Console.WriteLine("optimal value is" + res + "and optimal expected is"+optimalValue);
			Console.WriteLine(WrapperCoin.GetColName(problem, 6));
			WrapperCoin.UnloadProblem(problem);
			WrapperCoin.FreeSolver();

			res = WrapperCoin.GetObjectValue(problem);
			double[] activity = new double[22];
			double[] reducedCost = new double[22];
			double[] slackValues = new double[22+1];
			int[] shadowPrice = new int[22];
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			IntPtr pp = WrapperCoin.AddRow(problem, d, 1, 'L', na);
			problem.setProblem(pp);
			WrapperCoin.OptimizeProblem(problem);
			WrapperCoin.GetSolutionValues(problem, activity, reducedCost, slackValues, shadowPrice);
			res = WrapperCoin.GetObjectValue(problem);
			cc2 = WrapperCoin.GetRowCount(problem);
			string c11 = WrapperCoin.GetColName(problem, 0);/*/

			return 0;
		}

		static void printResult(double result, double[] activity)
		{
			Console.WriteLine($"The result is: {result}");
			string ac = string.Join(",", activity);
			Console.WriteLine($"The activity is: {ac}");
		}
	}
}

