using QRCoder;

namespace TVS_App.Infrastructure.Services;

public static class GenerateQrCodeService
{
    public static byte[] GenerateImage(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}