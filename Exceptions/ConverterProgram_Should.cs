using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Exceptions
{
    [TestFixture]
    public class ConverterProgram_Should : ReportingTest<ConverterProgram_Should>
    {
        // ReSharper disable once UnusedMember.Global
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
            Arrange(
                new Settings { SourceCultureName = sourceCulture, Verbose = false },
                input
            );

            ConverterProgram.Main("text.txt");

            Assert.IsTrue(File.Exists("text.txt.out"));
            Assert.IsEmpty(log.Logs);
        }

        [TestCase("2017-01-01", TestName = "correct date")]
        [TestCase("123", TestName = "correct number")]
        public void ConvertFast(string input)
        {
            #region warm_up
            ConverterProgram.ConvertLine(input);
            DateTime.TryParse(input, out var d1);
            #endregion

            var time = Measure(input, s => DateTime.TryParse(s, out var d) ? d.ToString() : s);
            var time2 = Measure(input, ConverterProgram.ConvertLine);
            Console.WriteLine(time);
            Console.WriteLine(time2);
            Assert.Less(time2.TotalMilliseconds, 10*time.TotalMilliseconds, "ConvertLine is too slow! (more than 10 times slower than just DateTime.TryParse)");
        }

        private static TimeSpan Measure(string input, Func<string, string> action)
        {
            var timer = Stopwatch.StartNew();
            for (var i = 0; i < 100000; i++)
            {
                action(input);
            }
            timer.Stop();
            return timer.Elapsed;
        }

        [Test]
        public void Fail_IfSettingslIncorrect()
        {
            File.WriteAllText("settings.xml", "NOT XML AT ALL!");

            ConverterProgram.Main();

            var errorMessage = log.Logs[0];
            Assert.That(errorMessage, Does.Match("Не удалось прочитать файл настроек"));
            Assert.That(errorMessage, Does.Match("XmlException"));
            Assert.That(log.Logs.Count, Is.EqualTo(1));
        }

        [Test]
        public void Fail_WhenNoFile()
        {
            Arrange(Settings.Default, "123");
            var filename = Guid.NewGuid().ToString();
            ConverterProgram.Main(filename);

            var errorMessage = log.Logs[0];
            Assert.That(errorMessage, Does.Match($"Не удалось сконвертировать {filename}"));
            Assert.That(errorMessage, Does.Match("FileNotFoundException"));
            Assert.AreEqual(1, log.Logs.Count);
        }

        [TestCase("abracadabra", TestName = "abracadabra")]
        [TestCase("100500 a", TestName = "wrong char index")]
        public void FailOn(string input)    
        {
            Arrange(Settings.Default, input);

            ConverterProgram.Main();

            var errorMessage = log.Logs[0];
            Assert.That(errorMessage, Does.Not.Match("AggregateException"));
            Assert.That(errorMessage, Does.Match("Некорректная строка"));
            Assert.AreEqual(1, log.Logs.Count);
        }

        [Test]
        public void UseDefaultSettings_IfNoSettings() 
        {
            Arrange(Settings.Default, "123");
            File.Delete("settings.xml");

            ConverterProgram.Main();

            Assert.That(log.Logs[0], Does.Match("Файл настроек .* отсутствует."));
            Assert.That(log.Logs.Count, Is.EqualTo(1));
            Assert.IsTrue(File.Exists("text.txt.out"));
        }

        private void Arrange(Settings settings, string input)
        {
            SaveSettings(settings);
            File.WriteAllText("text.txt", input);
        }

        private static void SaveSettings(Settings settings)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            using (var stream = new FileStream("settings.xml", FileMode.OpenOrCreate))
            {
                serializer.Serialize(stream, settings);
            }
        }

    }
}