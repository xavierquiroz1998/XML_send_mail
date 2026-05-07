namespace XmlEmailSender.Domain.Documents;

/// <summary>
/// Resolución del &lt;codigoPorcentaje&gt; del SRI (tabla 18) a una etiqueta
/// humana para el RIDE. NUNCA se hardcodea un porcentaje: el código manda.
/// </summary>
public static class IvaCodeLabels
{
    /// <summary>
    /// Devuelve una etiqueta para un código de porcentaje del SRI.
    /// Si el código no está mapeado, devuelve "IVA" (sin porcentaje) en lugar de mentir.
    /// </summary>
    public static string Resolve(string? codigoPorcentaje) => codigoPorcentaje switch
    {
        "0" => "IVA 0%",
        "2" => "IVA 12%",        // legacy, anterior a 2024
        "3" => "IVA 14%",
        "4" => "IVA 15%",        // estándar 2024+
        "5" => "IVA 5%",
        "6" => "No Objeto IVA",
        "7" => "Exento de IVA",
        "8" => "IRBPNR",         // botellas plásticas no retornables
        _   => "IVA"
    };
}
