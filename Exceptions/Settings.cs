namespace Exceptions;

public record Settings(string SourceCultureName, bool Verbose)
{
    public static readonly Settings Default = new("ru", false);
}