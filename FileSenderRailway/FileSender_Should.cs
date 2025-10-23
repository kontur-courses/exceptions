using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using VerifyNUnit;
using VerifyTests;

namespace FileSenderRailway;

[TestFixture]
public class FileSender_Should
{
    private FileSender fileSender;
    private ICryptographer cryptographer;
    private ISender sender;
    private IRecognizer recognizer;

    private readonly FileContent file = new(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToByteArray());
    private readonly DateTime now = new(2000, 01, 01);
    private readonly X509Certificate certificate = new();

    [SetUp]
    public void SetUp()
    {
        cryptographer = A.Fake<ICryptographer>();
        sender = A.Fake<ISender>();
        recognizer = A.Fake<IRecognizer>();
        fileSender = new FileSender(cryptographer, sender, recognizer, () => now);
    }

    [Test]
    public void BeOk_WhenGoodFormat(
        [Values("4.0", "3.1")] string format,
        [Values(0, 30)] int daysBeforeNow)
    {
        var signed = SomeByteArray();
        PrepareDocument(file, signed, now.AddDays(-daysBeforeNow), format);

        fileSender.SendFiles(new[] { file }, certificate)
            .Should().BeEquivalentTo(new[] { new FileSendResult(file) });
        A.CallTo(() => sender.Send(A<Document>.That.Matches(d => d.Content == signed)))
            .MustHaveHappened();
    }


    [Test]
    public async Task Fail_WhenNotRecognized()
    {
        A.CallTo(() => recognizer.Recognize(file))
            .Throws(new FormatException("Can't recognize"));

        await VerifyErrorOnPrepareFile(file, certificate);
    }

    [TestCase("1.0", 0)]
    [TestCase("4.0", 32)]
    [TestCase("3.1", 32)]
    [TestCase("wrong", 32)]
    [Test]
    public Task Fail_WhenBadFormatOrTimestamp(string format, int daysBeforeNow)
    {
        PrepareDocument(file, null, now.AddDays(-daysBeforeNow), format);
        return VerifyErrorOnPrepareFile(file, certificate, format, daysBeforeNow);
    }

    private void PrepareDocument(FileContent content, byte[] signedContent, DateTime created, string format)
    {
        var document = new Document(content.Name, content.Content, created, format);
        A.CallTo(() => recognizer.Recognize(content)).Returns(document);
        A.CallTo(() => cryptographer.Sign(content.Content, certificate)).Returns(signedContent);
    }

    private Task VerifyErrorOnPrepareFile(FileContent fileContent, X509Certificate x509Certificate, string format = null, int? daysBeforeNow = null)
    {
        var res = fileSender
            .SendFiles(new[] { fileContent }, x509Certificate)
            .Single();
        res.IsSuccess.Should().BeFalse();

        var settings = new VerifySettings();
        if (format != null && daysBeforeNow.HasValue)
        {
            settings.UseParameters(format, daysBeforeNow.Value);
        }
        return Verifier.Verify(res.Error, settings);
    }

    private static byte[] SomeByteArray()
    {
        return Guid.NewGuid().ToByteArray();
    }
}