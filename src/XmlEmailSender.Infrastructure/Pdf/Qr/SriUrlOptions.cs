namespace XmlEmailSender.Infrastructure.Pdf.Qr;

/// <summary>
/// Endpoints públicos del SRI para consulta de comprobantes electrónicos.
/// Configurables desde appsettings (sección "Sri:Urls") por si el SRI
/// cambia el dominio o queremos usar el portal web público en lugar del móvil.
/// </summary>
public sealed class SriUrlOptions
{
    public const string SectionName = "Sri:Urls";

    /// <summary>
    /// URL base de consulta pública para AMBIENTE = PRODUCCIÓN.
    /// Se le concatena ?clave=&lt;49 dígitos&gt;.
    /// </summary>
    public string ProductionConsultUrl { get; set; }
        = "https://srienlinea.sri.gob.ec/movil-servicios/api/v1.0/comprobantes/consultaPublica?clave=";

    /// <summary>
    /// URL base de consulta pública para AMBIENTE = PRUEBAS.
    /// </summary>
    public string TestingConsultUrl { get; set; }
        = "https://celcer.sri.gob.ec/comprobantes-electronicos-internet/pages/consultas/comprobante/consultaComprobanteElectronicoInternet.jsf?clave=";
}
