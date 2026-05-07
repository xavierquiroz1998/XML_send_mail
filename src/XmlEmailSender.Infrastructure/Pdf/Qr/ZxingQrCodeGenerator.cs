using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace XmlEmailSender.Infrastructure.Pdf.Qr;

/// <summary>
/// QR PNG mediante ZXing.Net + SkiaSharp para el rasterizado.
/// Implementación manual: ZXing genera la matriz de bits, SkiaSharp pinta el PNG.
/// </summary>
internal sealed class ZxingQrCodeGenerator : IQrCodeGenerator
{
    public byte[] Generate(string content, int sizePx = 220)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Contenido del QR no puede estar vacío.", nameof(content));

        var writer = new QRCodeWriter();
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.CHARACTER_SET] = "UTF-8",
            [EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.M,
            [EncodeHintType.MARGIN] = 1
        };

        BitMatrix matrix = writer.encode(content, BarcodeFormat.QR_CODE, sizePx, sizePx, hints);

        using var bitmap = new SKBitmap(matrix.Width, matrix.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = false };
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                    if (matrix[x, y])
                        canvas.DrawPoint(x, y, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
