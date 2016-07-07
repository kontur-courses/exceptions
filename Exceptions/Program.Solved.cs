using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using NLog;

namespace Exceptions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var filenames = args;
                var settings = LoadSettings();
                ConvertFiles(filenames, settings);
            }
            catch (Exception e)
            {
                LogManager.GetCurrentClassLogger().Error(e);
                Console.WriteLine(e.Message);
            }
        }

        private static void ConvertFiles(string[] filenames, Settings settings)
        {
            var threads = filenames.Select(fn => new Thread(() => ConvertFile(fn, settings))).ToList();
            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());
        }

        private static Settings LoadSettings()
        {
            var serializer = new XmlSerializer(typeof(Settings));
            var content = File.ReadAllText("settings.xml");
            return (Settings) serializer.Deserialize(new StringReader(content));
            // Если файла нет —значение по умолчанию.
            // Если ошибка формата — перевыбросить
        }

        private static void ConvertFile(string filename, Settings settings)
        {
            if (settings.SourceCultureName != null)
                Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.SourceCultureName);
            if (settings.Verbose)
            {
                Console.WriteLine("Processing file " + filename);
                Console.WriteLine("Source Culture " + Thread.CurrentThread.CurrentCulture.Name);
            }
            IEnumerable<string> lines;
            try
            {
                lines = PrepareLines(filename);
            }
            catch
            {
                // Это лишнее. Оно не работает и не нужно, даже если бы работало.
                Console.WriteLine($"File {filename} not found");
                return;
            }
            File.WriteAllLines(filename + ".out", lines.Select(ConvertLine));
            // Тут обработка ошибки в общем-то не нужна. Разве что перевыброс с доп-инфой.
        }

        private static IEnumerable<string> PrepareLines(string filename)
        {
            var lineIndex = 0;
            foreach (var line in File.ReadLines(filename))
            {
                if (line == "") continue;
                yield return line.Trim();
                lineIndex++;
            }
            yield return lineIndex.ToString();
        }

        private static string ConvertLine(string arg)
        {
            // Уточняющий перевыброс
            // TryParse
            try
            {
                return ConvertAsDateTime(arg);
            }
            catch
            {
                try
                {
                    return ConvertAsDouble(arg);
                }
                catch
                {
                    return ConvertAsCharIndexInstruction(arg);
                }
            }
        }

        private static string ConvertAsCharIndexInstruction(string s)
        {
            var parts = s.Split();
            var charIndex = int.Parse(parts[0]);
            var text = parts[1];
            return text[charIndex].ToString();
        }

        private static string ConvertAsDateTime(string arg)
        {
            return DateTime.Parse(arg).ToString(CultureInfo.InvariantCulture);
        }

        private static string ConvertAsDouble(string arg)
        {
            return double.Parse(arg).ToString(CultureInfo.InvariantCulture);
        }
    }
}