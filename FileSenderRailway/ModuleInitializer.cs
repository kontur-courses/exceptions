using System.IO;
using System.Runtime.CompilerServices;
using VerifyNUnit;

namespace FileSenderRailway;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        Verifier.DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.GetDirectoryName(sourceFile),
                typeName: type.Name,
                methodName: method.Name));
    }
}
