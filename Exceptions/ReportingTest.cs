using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Exceptions;

public static class Firebase
{
    public static FirebaseClient CreateClient() => new(BuildBaseUrl());

    private static string BuildBaseUrl()
    {
        const string Url = "https://testing-challenge.firebaseio.com";
        const string Realm = "exceptions";
        var dateKey = DateTime.Now.Date.ToString("yyyyMMdd");

        return $"{Url}/{Realm}/{dateKey}";
    }
}

public class ReportingTest<TTestClass>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string resultsFileName = typeof(TTestClass).Name + ".json";
    private static string resultsFile;
    private static List<TestCaseStatus> tests;

    [OneTimeSetUp]
    public async Task ClearLocalResults()
    {
        resultsFile = Path.Combine(TestContext.CurrentContext.TestDirectory, resultsFileName);
        tests = await LoadResults();
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
            testStatus.LastRunTime = DateTime.Now;
            testStatus.Succeeded = succeeded;
        }
        else
            tests.Add(new TestCaseStatus
            {
                FirstRunTime = DateTime.Now,
                LastRunTime = DateTime.Now,
                TestName = testName,
                TestMethod = test.MethodName,
                Succeeded = succeeded
            });
    }

    private static async Task SaveResults(List<TestCaseStatus> tests)
    {
        await using var stream = new FileStream(resultsFile, FileMode.Create);
        await JsonSerializer.SerializeAsync(stream, tests, JsonOptions);
    }

    private static async Task<List<TestCaseStatus>> LoadResults()
    {
        try
        {
            await using var stream = new FileStream(resultsFile, FileMode.Open);
            var statuses = await JsonSerializer.DeserializeAsync<List<TestCaseStatus>>(stream);
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
        var names = typeof(TTestClass).GetMethods().Select(m => m.Name).ToHashSet();
        return statuses.Where(s => names.Contains(s.TestMethod)).ToList();
    }

    [OneTimeTearDown]
    public static async Task ReportResults()
    {
        tests = tests.OrderByDescending(t => t.LastRunTime).ThenByDescending(t => t.FirstRunTime).ToList();
        await SaveResults(tests);
        var names = typeof(TTestClass).GetField("Names").GetValue(null);
        Console.WriteLine(names);
        foreach (var kv in tests)
        {
            Console.WriteLine(kv.TestName);
        }
        
        using (var client = Firebase.CreateClient())
        {
            await client.Child(names + "/tests").PutAsync(tests);
        }
        Console.WriteLine("reported");
    }
}

public class TestCaseStatus
{
    public string TestMethod;
    public string TestName;
    public DateTime FirstRunTime;
    public DateTime LastRunTime;
    public bool Succeeded;
}