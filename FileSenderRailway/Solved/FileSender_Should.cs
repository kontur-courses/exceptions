using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using VerifyNUnit;
using VerifyTests;

namespace FileSenderRailway.Solved;

[TestFixture]
public class FileSender_Should
{
    private FileSender fileSender;
    private ICryptographer cryptographer;
    private IRecognizer recognizer;

    [SetUp]
    public void SetUp()
    {
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
        cryptographer = A.Fake<ICryptographer>();
        recognizer = A.Fake<IRecognizer>();
        fileSender = new FileSender(cryptographer, A.Fake<ISender>(), recognizer, () => now);
    }

    private readonly FileContent file = new(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToByteArray());
    private readonly DateTime now = new(2000, 01, 01);
    private readonly X509Certificate certificate = new();

    [Test]
    public void BeOk_WhenGoodFormat(
        [Values("4.0", "3.1")]string format,
        [Values(0, 30)]int daysBeforeNow)
    {
        var signedContent = SomeByteArray();
        var document = PrepareDocument(file, signedContent, now.AddDays(-daysBeforeNow), format);
        var expectedDocument = document with { Content = signedContent };
        var result = fileSender.PrepareFileToSend(file, certificate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDocument);
    }

    [Test]
    public async Task Fail_WhenNotRecognized()
    {
        A.CallTo(() => recognizer.Recognize(file))
            .Returns(Result.Fail<Document>("Can't recognize"));

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

    private Document PrepareDocument(FileContent fileToPrepare, byte[] signed, DateTime created, string format)
    {
        var document = new Document(fileToPrepare.Name, fileToPrepare.Content, created, format);
        A.CallTo(() => recognizer.Recognize(fileToPrepare)).Returns(Result.Ok(document));
        A.CallTo(() => cryptographer.Sign(fileToPrepare.Content, certificate)).Returns(signed);
        return document;
    }

    private Task VerifyErrorOnPrepareFile(FileContent fileContent, X509Certificate x509Certificate, string format = null, int? daysBeforeNow = null)
    {
        var res = fileSender.PrepareFileToSend(fileContent, x509Certificate);
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