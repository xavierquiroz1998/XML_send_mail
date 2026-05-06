namespace XmlEmailSender.Domain.Documents;

public sealed class DocumentLine
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ElectronicDocumentId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Subtotal { get; private set; }

    private DocumentLine() { }

    public DocumentLine(
        string code,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal discount,
        decimal subtotal)
    {
        Code = code;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = discount;
        Subtotal = subtotal;
    }

    internal void AttachTo(Guid documentId) => ElectronicDocumentId = documentId;
}
