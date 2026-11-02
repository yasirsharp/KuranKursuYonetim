using System.Net;
using System.Xml.Linq;
using HafizTakip.Core.Utilities.Sms;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HafizTakip.Business.Tests.Common;

public class NetGsmSmsSenderTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond(request);
        }
    }

    private static HttpResponseMessage Xml(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body) };

    private static (NetGsmSmsSender Sender, FakeHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new FakeHandler(respond);
        var settings = Options.Create(new SmsSettings
        {
            Provider = SmsSettings.NetGsmProvider,
            NetGsm = new NetGsmSettings { UserCode = "8508001122", Password = "a<b&c", Header = "HAFIZTAKIP" }
        });
        return (new NetGsmSmsSender(new HttpClient(handler), settings, NullLogger<NetGsmSmsSender>.Instance), handler);
    }

    [Fact]
    public async Task Send_ShouldPostOtpXmlWithEscapedCredentialsNationalNumberAndAsciiText()
    {
        var (sender, handler) = Create(_ => Xml("<xml><main><code>0</code><jobID>1310546758</jobID></main></xml>"));

        var ok = await sender.SendAsync("0532 111 22 33", "Hafız Takip giriş kodunuz: 123456. Kimseyle paylaşmayın.");

        ok.Should().BeTrue();
        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.ToString().Should().Be("https://api.netgsm.com.tr/sms/send/otp");
        handler.Request.Content!.Headers.ContentType!.MediaType.Should().Be("text/xml");

        var doc = XDocument.Parse(handler.Body!);
        doc.Root!.Name.LocalName.Should().Be("mainbody");
        doc.Root.Element("header")!.Element("usercode")!.Value.Should().Be("8508001122");
        doc.Root.Element("header")!.Element("password")!.Value.Should().Be("a<b&c", "şifredeki özel karakterler XML'i bozmamalı");
        doc.Root.Element("header")!.Element("msgheader")!.Value.Should().Be("HAFIZTAKIP");
        doc.Root.Element("body")!.Element("no")!.Value.Should().Be("5321112233");
        doc.Root.Element("body")!.Element("msg")!.Value.Should().Be("Hafiz Takip giris kodunuz: 123456. Kimseyle paylasmayin.");
    }

    [Theory]
    [InlineData("60")]
    [InlineData("30")]
    [InlineData("73")]
    [InlineData("80")]
    public async Task Send_ErrorCode_ShouldReturnFalse(string code)
    {
        var (sender, _) = Create(_ => Xml($"<xml><main><code>{code}</code></main></xml>"));

        (await sender.SendAsync("5321112233", "Kod: 123456")).Should().BeFalse();
    }

    [Fact]
    public async Task Send_UnreadableResponseOrServerError_ShouldReturnFalse()
    {
        var (garbage, _) = Create(_ => Xml("sunucu hatası"));
        (await garbage.SendAsync("5321112233", "Kod: 123456")).Should().BeFalse();

        var (serverError, _) = Create(_ => Xml("<xml><main><code>0</code></main></xml>", HttpStatusCode.InternalServerError));
        (await serverError.SendAsync("5321112233", "Kod: 123456")).Should().BeFalse();
    }

    [Fact]
    public async Task Send_NetworkFailure_ShouldReturnFalseNotThrow()
    {
        var (sender, _) = Create(_ => throw new HttpRequestException("bağlantı yok"));

        (await sender.SendAsync("5321112233", "Kod: 123456")).Should().BeFalse();
    }

    [Theory]
    [InlineData("<xml><main><code>0</code><jobID>1</jobID></main></xml>", true, "0", null)]
    [InlineData("<xml><main><jobID>1</jobID></main></xml>", true, null, null)]
    [InlineData("<xml><main><code>41</code></main></xml>", false, "41", null)]
    [InlineData("<xml><main></main></xml>", false, null, null)]
    // NetGSM'in yanlış bilgiyle verdiği gerçek yanıt
    [InlineData("<?xml version=\"1.0\"?>\n<xml><main><code>30</code><error>Kullanici bilgisi bulunamadi</error></main></xml>",
        false, "30", "Kullanici bilgisi bulunamadi")]
    public void ParseResponse_ShouldReadCodeJobIdAndError(string body, bool success, string? code, string? error)
    {
        NetGsmSmsSender.ParseResponse(body).Should().Be((success, code, error));
    }

    [Fact]
    public void RealMessages_ShouldFitInOneOtpSms()
    {
        // AuthService ve CourseApplicationManager'daki en uzun metinler (3 dakikalık kodla)
        var login = NetGsmSmsSender.ToOtpText("Hafız Takip giriş kodunuz: 123456. Kod 3 dakika geçerlidir. Kimseyle paylaşmayın.");
        var application = NetGsmSmsSender.ToOtpText("Hafız Takip kurs başvurusu doğrulama kodunuz: 123456. Kod 3 dakika geçerlidir. Kimseyle paylaşmayın.");

        login.Length.Should().BeLessOrEqualTo(NetGsmSmsSender.MaxLength);
        application.Length.Should().BeLessOrEqualTo(NetGsmSmsSender.MaxLength);
        (login + application).Should().MatchRegex("^[\\x20-\\x7E]+$", "OTP kanalı Türkçe karakter desteklemez");
    }
}
