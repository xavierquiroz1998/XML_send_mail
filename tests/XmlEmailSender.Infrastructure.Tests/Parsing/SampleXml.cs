namespace XmlEmailSender.Infrastructure.Tests.Parsing;

internal static class SampleXml
{
    /// <summary>
    /// Clave de acceso de 49 dígitos con dígito verificador correcto (módulo 11).
    /// Calculada con BuildValidAccessKey() para los 48 primeros dígitos = "1".
    /// </summary>
    public static readonly string ValidAccessKey = BuildValidAccessKey(new string('1', 48));

    public static string BuildValidAccessKey(string firstFortyEight)
    {
        if (firstFortyEight.Length != 48 || !firstFortyEight.All(char.IsDigit))
            throw new ArgumentException("Se requieren exactamente 48 dígitos.");

        int[] weights = { 2, 3, 4, 5, 6, 7 };
        int sum = 0;
        int weightIndex = 0;

        for (int i = firstFortyEight.Length - 1; i >= 0; i--)
        {
            sum += int.Parse(firstFortyEight[i].ToString()) * weights[weightIndex];
            weightIndex = (weightIndex + 1) % weights.Length;
        }

        int mod = sum % 11;
        int check = 11 - mod;
        if (check == 11) check = 0;
        if (check == 10) check = 1;

        return firstFortyEight + check.ToString();
    }

    public static string MinimalInvoice(string accessKey, bool includeEmail = true)
    {
        var infoAdicional = includeEmail
            ? @"<infoAdicional>
                <campoAdicional nombre=""email"">cliente@ejemplo.com</campoAdicional>
                <campoAdicional nombre=""telefono"">0999999999</campoAdicional>
              </infoAdicional>"
            : string.Empty;

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<factura id=""comprobante"" version=""1.1.0"">
  <infoTributaria>
    <ambiente>2</ambiente>
    <tipoEmision>1</tipoEmision>
    <razonSocial>EMPRESA EJEMPLO S.A.</razonSocial>
    <nombreComercial>EJEMPLO</nombreComercial>
    <ruc>1790012345001</ruc>
    <claveAcceso>{accessKey}</claveAcceso>
    <codDoc>01</codDoc>
    <estab>001</estab>
    <ptoEmi>001</ptoEmi>
    <secuencial>000000123</secuencial>
    <dirMatriz>Av. Amazonas 123</dirMatriz>
  </infoTributaria>
  <infoFactura>
    <fechaEmision>25/02/2024</fechaEmision>
    <obligadoContabilidad>SI</obligadoContabilidad>
    <tipoIdentificacionComprador>05</tipoIdentificacionComprador>
    <razonSocialComprador>Juan Pérez</razonSocialComprador>
    <identificacionComprador>1712345678</identificacionComprador>
    <totalSinImpuestos>100.00</totalSinImpuestos>
    <totalDescuento>0.00</totalDescuento>
    <totalConImpuestos>
      <totalImpuesto>
        <codigo>2</codigo>
        <codigoPorcentaje>2</codigoPorcentaje>
        <baseImponible>100.00</baseImponible>
        <valor>12.00</valor>
      </totalImpuesto>
    </totalConImpuestos>
    <propina>0.00</propina>
    <importeTotal>112.00</importeTotal>
    <moneda>DOLAR</moneda>
  </infoFactura>
  <detalles>
    <detalle>
      <codigoPrincipal>SKU-001</codigoPrincipal>
      <descripcion>Producto de prueba</descripcion>
      <cantidad>1.00</cantidad>
      <precioUnitario>100.00</precioUnitario>
      <descuento>0.00</descuento>
      <precioTotalSinImpuesto>100.00</precioTotalSinImpuesto>
    </detalle>
  </detalles>
  {infoAdicional}
</factura>";
    }

    /// <summary>
    /// Factura con dos buckets de IVA (0% + 15%) para validar el desglose.
    /// </summary>
    public static string MinimalInvoiceWithMixedTaxes(string accessKey)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<factura id=""comprobante"" version=""1.1.0"">
  <infoTributaria>
    <ambiente>2</ambiente>
    <tipoEmision>1</tipoEmision>
    <razonSocial>EMPRESA EJEMPLO S.A.</razonSocial>
    <ruc>1790012345001</ruc>
    <claveAcceso>{accessKey}</claveAcceso>
    <codDoc>01</codDoc>
    <estab>001</estab>
    <ptoEmi>001</ptoEmi>
    <secuencial>000000999</secuencial>
    <dirMatriz>Av. Amazonas 123</dirMatriz>
  </infoTributaria>
  <infoFactura>
    <fechaEmision>15/05/2026</fechaEmision>
    <obligadoContabilidad>SI</obligadoContabilidad>
    <tipoIdentificacionComprador>05</tipoIdentificacionComprador>
    <razonSocialComprador>Juan Pérez</razonSocialComprador>
    <identificacionComprador>1712345678</identificacionComprador>
    <totalSinImpuestos>200.00</totalSinImpuestos>
    <totalDescuento>0.00</totalDescuento>
    <totalConImpuestos>
      <totalImpuesto>
        <codigo>2</codigo>
        <codigoPorcentaje>0</codigoPorcentaje>
        <baseImponible>100.00</baseImponible>
        <valor>0.00</valor>
      </totalImpuesto>
      <totalImpuesto>
        <codigo>2</codigo>
        <codigoPorcentaje>4</codigoPorcentaje>
        <baseImponible>100.00</baseImponible>
        <valor>15.00</valor>
      </totalImpuesto>
    </totalConImpuestos>
    <propina>0.00</propina>
    <importeTotal>215.00</importeTotal>
    <moneda>DOLAR</moneda>
  </infoFactura>
  <detalles>
    <detalle>
      <codigoPrincipal>SKU-A</codigoPrincipal>
      <descripcion>Producto exento</descripcion>
      <cantidad>1.00</cantidad>
      <precioUnitario>100.00</precioUnitario>
      <descuento>0.00</descuento>
      <precioTotalSinImpuesto>100.00</precioTotalSinImpuesto>
    </detalle>
    <detalle>
      <codigoPrincipal>SKU-B</codigoPrincipal>
      <descripcion>Producto gravado</descripcion>
      <cantidad>1.00</cantidad>
      <precioUnitario>100.00</precioUnitario>
      <descuento>0.00</descuento>
      <precioTotalSinImpuesto>100.00</precioTotalSinImpuesto>
    </detalle>
  </detalles>
</factura>";
    }
}
