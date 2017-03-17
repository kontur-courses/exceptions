using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FireSharp;
using FireSharp.Config;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace KonturCSharper
{
	public class ReportingTest<TTestClass>
	{
		private static readonly string resultsFileName = typeof(TTestClass).Name + ".txt";
		private static string resultsFile;
		private static List<TestCaseStatus> tests;

		[OneTimeSetUp]
		public void ClearLocalResults()
		{
			resultsFile = Path.Combine(TestContext.CurrentContext.TestDirectory, resultsFileName);
			tests = LoadResults();
		}


		[TearDown]
		public static void WriteLastRunResult()
		{
			var test = TestContext.CurrentContext.Test;
			var status = TestContext.CurrentContext.Result.Outcome.Status;
			var succeeded = status == TestStatus.Passed;
			var testName = test.Name;
			if (!test.Name.Contains(test.MethodName)) testName = test.MethodName + " " + test.Name;
			var testStatus = tests.FirstOrDefault(t => t.TestName == testName);
			if (testStatus != null)
			{
				testStatus.LastTime = DateTime.Now;
				testStatus.Succeeded = succeeded;
			}
			else
				tests.Add(new TestCaseStatus
				{
					FirstTime = DateTime.Now,
					LastTime = DateTime.Now,
					TestName = testName,
					TestMethod = test.MethodName,
					Succeeded = succeeded
				});
		}

		private static void SaveResults(List<TestCaseStatus> tests)
		{
			File.WriteAllText(resultsFile, JsonConvert.SerializeObject(tests, Formatting.Indented));
		}

		private static List<TestCaseStatus> LoadResults()
		{
			try
			{
				var json = File.ReadAllText(resultsFile);
				var statuses = JsonConvert.DeserializeObject<List<TestCaseStatus>>(json);
				return RemoveOldNames(statuses);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return new List<TestCaseStatus>();
			}
		}

		private static List<TestCaseStatus> RemoveOldNames(List<TestCaseStatus> statuses)
		{
			var names = new HashSet<string>(typeof(TTestClass).GetMethods().Select(m => m.Name));
			return statuses.Where(s => names.Contains(s.TestMethod)).ToList();
		}

		[OneTimeTearDown]
		public static void ReportResults()
		{
			SaveResults(tests);
			var names = typeof(TTestClass).GetField("Names").GetValue(null);
			Console.WriteLine(names);
			foreach (var kv in tests)
			{
				Console.WriteLine(kv.TestName);
			}
			var config = new FirebaseConfig
			{
				BasePath = $"https://testing-challenge.firebaseio.com/testsboard/{typeof(TTestClass).Name}/{DateTime.Today:yyyyMMdd}"
			};
			using (var client = new FirebaseClient(config))
			{
				client.Set(names + "/tests", tests);
			}
			Console.WriteLine("reported");
		}
	}

	public class TestCaseStatus
	{
		public string TestMethod;
		public string TestName;
		public DateTime FirstTime;
		public DateTime LastTime;
		public bool Succeeded;
	}
}