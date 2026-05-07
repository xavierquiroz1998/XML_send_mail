namespace XmlEmailSender.Infrastructure.Pdf.Qr;

public interface IQrCodeGenerator
{
    /// <summary>
    /// Genera un PNG cuadrado con el contenido indicado.
    /// </summary>
    byte[] Generate(string content, int sizePx = 220);
}
