using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Exceptions.Solved;

public class Converter
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static void ConvertFiles(params string[] filenames)
    {
        ErrorHandler.LogErrors(() =>
        {
            filenames = filenames.Length > 0 ? filenames : new[] { "text.txt" };
            var settings = LoadSettings();
            ConvertFiles(filenames, settings);
        });
    }

    private static void ConvertFiles(string[] filenames, Settings settings)
    {
        var tasks = filenames
            .Select(fn => Task.Run(() => ConvertFile(fn, settings))
                .ContinueWith(LogIfException))
            .ToArray();
        Task.WaitAll(tasks);
    }

    private static void LogIfException(Task task)
    {
        var exceptions = task.Exception;
        if (!task.IsFaulted || exceptions == null) return;
        foreach (var ex in exceptions.InnerExceptions)
            log.Error(ex);
    }

    private static Settings LoadSettings()
    {
        var filename = "settings.json";
        if (!File.Exists(filename))
        {
            log.Info($"Файл настроек {filename} отсутствует. Используются настройки по умолчанию.");
            return Settings.Default;
        }
        try
        {
            using var stream = new FileStream(filename, FileMode.Open);
            return JsonSerializer.Deserialize<Settings>(stream);
        }
        catch (Exception e)
        {
            throw new FormatException($"Не удалось прочитать файл настроек {filename}", e);
        }
    }

    private static void ConvertFile(string filename, Settings settings)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.SourceCultureName);
        if (settings.Verbose)
        {
            log.Info("Processing file " + filename);
            log.Info("Source Culture " + Thread.CurrentThread.CurrentCulture.Name);
        }
        var lines = PrepareLines(filename);
        try
        {
            var convertedLines = lines
                .Select(ConvertLine)
                .Select(s => s.Length + " " + s);
            File.WriteAllLines(filename + ".out", convertedLines);
        }
        catch (Exception e)
        {
            throw new Exception($"Не удалось сконвертировать {filename}", e);
        }
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

    public static string ConvertLine(string line)
    {
        return ErrorHandler.Refine(() =>
            TryConvertAsDateTime(line)
            ?? TryConvertAsDouble(line)
            ?? ConvertAsCharIndexInstruction(line),
            e => new FormatException($"Некорректная строка [{line}]", e));
    }

    private static string ConvertAsCharIndexInstruction(string s)
    {
        var parts = s.Split();
        var charIndex = int.Parse(parts[0]);
        var text = parts[1];
        return text[charIndex].ToString();
    }

    private static string TryConvertAsDateTime(string arg)
    {
        return DateTime.TryParse(arg, out var res)
            ? res.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private static string TryConvertAsDouble(string arg)
    {
        return double.TryParse(arg, out var res)
            ? res.ToString(CultureInfo.InvariantCulture)
            : null;
    }
}