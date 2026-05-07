namespace XmlEmailSender.Domain.Documents;

/// <summary>
/// Desglose de un impuesto por <c>codigoPorcentaje</c> del SRI.
/// Una factura puede tener varias buckets (por ejemplo, líneas con IVA 0% e IVA 15%
/// en el mismo comprobante).
/// </summary>
/// <param name="CodigoPorcentaje">
/// Código del SRI (tabla 18). Algunos valores comunes: "0"=0%, "2"=12% (legacy),
/// "3"=14%, "4"=15% (vigente), "5"=5%, "6"=No objeto, "7"=Exento, "8"=IRBPNR.
/// </param>
/// <param name="BaseImponible">Suma de las bases imponibles que cayeron en este código.</param>
/// <param name="Valor">Valor recaudado del impuesto para esta bucket.</param>
public sealed record TaxBucket(
    string CodigoPorcentaje,
    decimal BaseImponible,
    decimal Valor);
