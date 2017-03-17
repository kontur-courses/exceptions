using System;
using System.IO;
using System.Xml.Serialization;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Exceptions
{
	[TestFixture]
	public class Program_Should
	{
		[SetUp]
		public void SetUp()
		{
			log = new MemoryTarget();
			SimpleConfigurator.ConfigureForTargetLogging(log);
			File.Delete("text.txt.out");
		}

		private MemoryTarget log;


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

		[TestCase("ru", "1")]
		[TestCase("ru", "1,12")]
		[TestCase("ru", "15.11.1982")]
		[TestCase("ru", "1\n1,12\n15.11.1982")]
		[TestCase("en", "1.12")]
		[TestCase("en", "12/31/2017")]
		public void ConvertSilently(string sourceCulture, string input)
		{
			Arrange(
				new Settings { SourceCultureName = sourceCulture, Verbose = false },
				input
			);

			Program.Main(new[] { "text.txt" });

			Assert.IsTrue(File.Exists("text.txt.out"));
			Assert.IsEmpty(log.Logs);
		}

		[Test, Explicit("Что-то сломалось...")]
		public void GracefullyFail_WhenFileNotFound()
		{
			var filename = Guid.NewGuid().ToString();
			Program.Main(new[] { filename });

			Assert.AreEqual(1, log.Logs.Count);
			Assert.That(log.Logs[0], Does.Match($"File {filename} not found"));
		}

		[Test, Explicit("Что-то непонятное...")]
		public void GracefullyFail_WhenFormatIsWrong()
		{
			Arrange(
				new Settings { SourceCultureName = "ru", Verbose = false },
				"12qwe"
			);

			Program.Main();

			Assert.AreEqual(1, log.Logs.Count);
			Assert.That(log.Logs[0], Does.Not.Match("AggregateException"));
			Assert.That(log.Logs[0], Does.Not.Match("NullReferenceException"));
		}

		[Test, Explicit("Надо доделать...")]
		public void DefaultSettings_IfSettingsXmlAbsent()
		{
			Arrange(new Settings(), "123");
			File.Delete("settings.xml");

			Program.Main();

			Assert.That(log.Logs.Count, Is.EqualTo(1));
			Assert.That(log.Logs[0], Does.Match("Файл настроек .* отсутствует."));
		}
	}
}