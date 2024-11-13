using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Exceptions;

[TestFixture]
public class Converter_Should : ReportingTest<Converter_Should>
{
    // ReSharper disable once UnusedMember.Global
    #pragma warning disable CA2211 // Non-constant fields should not be visible
    public static string Names = "ВАШИ ФАМИЛИИ ЧЕРЕЗ ПРОБЕЛ"; // Ivanov Petrov

    private MemoryTarget log;

    [SetUp]
    public void SetUp()
    {
        log = new MemoryTarget();
        SimpleConfigurator.ConfigureForTargetLogging(log);
        log.Layout = "${longdate} ${uppercase:${level}} ${message} ${exception:format=tostring}";
        // Uncomment the next line if tests fails due to UnauthorizedAccessException
        // Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        File.Delete("text.txt.out");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var message in log.Logs)
            Console.WriteLine(message);
    }

    [TestCase("ru", "1,12", TestName = "double")]
    [TestCase("ru", "15.11.1982", TestName = "date")]
    [TestCase("ru", "1 asdasd", TestName = "char")]
    [TestCase("ru", "1\n1,12\n15.11.1982\n1 qwe", TestName = "mixed")]
    [TestCase("en", "1.12", TestName = "en double")]
    [TestCase("en", "12/31/2017", TestName = "en date")]
    public void Convert(string sourceCulture, string input)
    {
        Arrange(new Settings(sourceCulture, Verbose: false), input);

        Converter.ConvertFiles("text.txt");

        Assert.That(File.Exists("text.txt.out"));
        Assert.That(log.Logs, Is.Empty);
    }

    [TestCase("2017-01-01", TestName = "correct date")]
    [TestCase("123", TestName = "correct number")]
    public void ConvertFast(string input)
    {
        #region warm_up
        Converter.ConvertLine(input);
        DateTime.TryParse(input, out _);
        #endregion

        var time = Measure(input, s => DateTime.TryParse(s, out var d) ? d.ToString() : s);
        var time2 = Measure(input, Converter.ConvertLine);
        Console.WriteLine(time);
        Console.WriteLine(time2);
        Assert.That(time2.TotalMilliseconds, Is.LessThan(10 * time.TotalMilliseconds), "ConvertLine is too slow! (more than 10 times slower than just DateTime.TryParse)");
    }

    [Test]
    public void Fail_WhenIncorrectSettingsFile()
    {
        File.WriteAllText("settings.json", "NOT JSON AT ALL!");

        Converter.ConvertFiles();

        var errorMessage = log.Logs[0];
        Assert.That(errorMessage, Does.Match("Не удалось прочитать файл настроек"));
        Assert.That(errorMessage, Does.Match("JsonException"));
        Assert.That(log.Logs.Count, Is.EqualTo(1));
    }

    [Test]
    public void Fail_WhenNoSettingsFile()
    {
        Arrange(Settings.Default, "123");
        var filename = Guid.NewGuid().ToString();
        Converter.ConvertFiles(filename);

        var errorMessage = log.Logs[0];
        Assert.That(errorMessage, Does.Match($"Не удалось сконвертировать {filename}"));
        Assert.That(errorMessage, Does.Match("FileNotFoundException"));
        Assert.That(log.Logs.Count, Is.EqualTo(1));
    }

    [TestCase("abracadabra", TestName = "abracadabra")]
    [TestCase("100500 a", TestName = "wrong char index")]
    public void FailOn(string input)    
    {
        Arrange(Settings.Default, input);

        Converter.ConvertFiles();

        var errorMessage = log.Logs[0];
        Assert.That(errorMessage, Does.Not.Match("AggregateException"));
        Assert.That(errorMessage, Does.Match("Некорректная строка"));
        Assert.That(log.Logs.Count, Is.EqualTo(1));
    }

    [Test]
    public void UseDefaultSettings_WhenNoSettingsFile() 
    {
        Arrange(Settings.Default, "123");
        File.Delete("settings.json");

        Converter.ConvertFiles();

        Assert.That(log.Logs[0], Does.Match("Файл настроек .* отсутствует."));
        Assert.That(log.Logs.Count, Is.EqualTo(1));
        Assert.That(File.Exists("text.txt.out"));
    }

    private static void Arrange(Settings settings, string input)
    {
        SaveSettings(settings);
        File.WriteAllText("text.txt", input);
    }

    private static void SaveSettings(Settings settings)
    {
        using var stream = new FileStream("settings.json", FileMode.Truncate);
        JsonSerializer.Serialize(stream, settings);
    }

    private static TimeSpan Measure(string input, Func<string, string> action)
    {
        var ts = Stopwatch.GetTimestamp();
        for (var i = 0; i < 100_000; i++)
            action(input);

        return Stopwatch.GetElapsedTime(ts);
    }
}