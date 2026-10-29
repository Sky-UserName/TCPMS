using QRCoder;

namespace TCPMS.Api.Infrastructure;

public static class VerificationQrCodeGenerator
{
    public static byte[] CreatePng(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(12);
    }
}
