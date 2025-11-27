using System;

namespace ResultOf;

public static class ParseResultExtensions
{
    public static Result<int> ParseIntResult(this string s, string error = null)
    {
        return int.TryParse(s, out var v)
            ? v.AsResult()
            : Result.Fail<int>(error ?? $"Не число {s}");
    }
    public static Result<Guid> ParseGuidResult(this string s, string error = null)
    {
        return Guid.TryParse(s, out var v)
            ? v.AsResult()
            : Result.Fail<Guid>(error ?? $"Не GUID {s}");
    }
}