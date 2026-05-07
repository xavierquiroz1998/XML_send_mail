namespace XmlEmailSender.Application.Documents.Dtos;

public sealed record DocumentDto(
    Guid Id,
    int Type,                    // DocumentType enum como int para el cliente HTTP
    string TypeName,             // "Invoice" / "CreditNote" / "WithholdingReceipt"
    string AccessKey,
    string DocumentNumber,
    DateTime IssueDate,
    string Environment,
    string IssuerRuc,
    string IssuerBusinessName,
    string ReceiverIdentification,
    string ReceiverName,
    string? ReceiverEmail,
    decimal Subtotal,
    decimal Taxes,
    decimal Total,
    IReadOnlyList<DocumentLineDto> Lines,
    IReadOnlyList<TaxBucketDto> TaxBreakdown);

public sealed record DocumentLineDto(
    string Code,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Subtotal);

public sealed record TaxBucketDto(
    string CodigoPorcentaje,
    string Label,                // resuelto por IvaCodeLabels (ej. "IVA 15%")
    decimal BaseImponible,
    decimal Valor);
