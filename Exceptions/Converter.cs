using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Exceptions;

public class Converter
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static void ConvertFiles(params string[] filenames)
    {
        try
        {
            filenames = filenames.Length > 0 ? filenames : new[] { "text.txt" };
            var settings = LoadSettings();
            ConvertFiles(filenames, settings);
        }
        catch (Exception e)
        {
            log.Error(e);
        }
    }

    private static void ConvertFiles(string[] filenames, Settings settings)
    {
        var tasks = filenames
            .Select(fn => Task.Run(() => ConvertFile(fn, settings)))
            .ToArray();
        Task.WaitAll(tasks);
    }

    private static Settings LoadSettings()
    {
        using var stream = new FileStream("settings.json", FileMode.Open);
        return JsonSerializer.Deserialize<Settings>(stream);
    }

    private static void ConvertFile(string filename, Settings settings)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.SourceCultureName);
        if (settings.Verbose)
        {
            log.Info("Processing file " + filename);
            log.Info("Source Culture " + Thread.CurrentThread.CurrentCulture.Name);
        }
        IEnumerable<string> lines;
        try
        {
            lines = PrepareLines(filename);
        }
        catch
        {
            log.Error($"File {filename} not found");
            return;
        }
        var convertedLines = lines
            .Select(ConvertLine)
            .Select(s => s.Length + " " + s);
        File.WriteAllLines(filename + ".out", convertedLines);
    }

    private static IEnumerable<string> PrepareLines(string filename)
    {
        var lineIndex = 0;
        foreach (var line in File.ReadLines(filename))
        {
            if (line == string.Empty) continue;
            yield return line.Trim();
            lineIndex++;
        }
        yield return lineIndex.ToString();
    }

    public static string ConvertLine(string arg)
    {
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
        if (parts.Length < 2) return null;
        var charIndex = int.Parse(parts[0]);
        if ((charIndex < 0) || (charIndex >= parts[1].Length))
            return null;
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
