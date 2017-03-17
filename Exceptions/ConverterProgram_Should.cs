using System;
using System.IO;
using System.Xml.Serialization;
using KonturCSharper;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Exceptions
{
	[TestFixture]
	public class ConverterProgram_Should : ReportingTest<ConverterProgram_Should>
	{
		// ReSharper disable once UnusedMember.Global
		public static string Names = "ФАШИ ФАМИЛИИ ЧЕРЕЗ ПРОБЕЛ"; // Ivanov Petrov

		private MemoryTarget log;

		[SetUp]
		public void SetUp()
		{
			log = new MemoryTarget();
			SimpleConfigurator.ConfigureForTargetLogging(log);
			File.Delete("text.txt.out");
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var message in log.Logs)
				Console.WriteLine(message);
		}


		[TestCase("ru", "1")]
		[TestCase("ru", "1,12")]
		[TestCase("ru", "15.11.1982")]
		[TestCase("ru", "1 asdasd")]
		[TestCase("ru", "1\n1,12\n15.11.1982\n1 qwe")]
		[TestCase("en", "1.12")]
		[TestCase("en", "12/31/2017")]
		public void ConvertSilently(string sourceCulture, string input)
		{
			Arrange(
				new Settings { SourceCultureName = sourceCulture, Verbose = false },
				input
			);

			ConverterProgram.Main("text.txt");

			Assert.IsTrue(File.Exists("text.txt.out"));
			Assert.IsEmpty(log.Logs);
		}

		[Test]
		public void FailGracefully_IfSettingsXmlIncorrect()
		{
			File.WriteAllText("settings.xml", "NOT XML AT ALL!");

			ConverterProgram.Main();

			var errorMessage = log.Logs[0];
			// должно быть понятное сообщение:
			Assert.That(errorMessage, Does.Match("Не удалось прочитать файл настроек"));
			// и технические подробности:
			Assert.That(errorMessage, Does.Match("XmlException"));
			Assert.That(log.Logs.Count, Is.EqualTo(1));
		}

		[Test]
		public void FailGracefully_WhenFileNotFound()
		{
			Arrange(Settings.Default, "123");
			var filename = Guid.NewGuid().ToString();
			ConverterProgram.Main(filename);

			var errorMessage = log.Logs[0];
			Assert.That(errorMessage, Does.Match($"Не удалось сконвертировать {filename}"));
			Assert.That(errorMessage, Does.Match("FileNotFoundException"));
			Assert.AreEqual(1, log.Logs.Count);
		}

		[TestCase("qwe123")]
		[TestCase("100500 a")]
		public void FailGracefully_WhenFormatIsWrong(string input)
		{
			Arrange(Settings.Default, input);

			ConverterProgram.Main();

			// Не должно быть трэша:
			var errorMessage = log.Logs[0];
			Assert.That(errorMessage, Does.Not.Match("AggregateException"));
			// Должны быть подробности про ошибку формата:
			Assert.That(errorMessage, Does.Match("Некорректная строка"));
			Assert.AreEqual(1, log.Logs.Count);
		}

		[Test]
		public void UseDefaultSettings_IfSettingsXmlAbsent()
		{
			Arrange(Settings.Default, "123");
			File.Delete("settings.xml");

			ConverterProgram.Main();

			//Должно быть понятное предупреждение:
			Assert.That(log.Logs[0], Does.Match("Файл настроек .* отсутствует."));
			Assert.That(log.Logs.Count, Is.EqualTo(1));
			//Но программа должна отработать с настройками по умолчанию:
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