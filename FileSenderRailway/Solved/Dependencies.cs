using System;
using System.Security.Cryptography.X509Certificates;

namespace FileSenderRailway.Solved
{
    public record Document(string Name, byte[] Content, DateTime Created, string Format);

    public record FileContent(string Name, byte[] Content);

    public record FileSendResult(FileContent File, string Error);

    public interface ICryptographer
    {
        byte[] Sign(byte[] content, X509Certificate certificate);
    }

    public interface IRecognizer
    {
        Result<Document> Recognize(FileContent file);
    }

    public interface ISender
    {
        Result<None> Send(Document content);
    }
}