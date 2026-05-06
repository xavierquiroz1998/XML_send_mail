namespace XmlEmailSender.Domain.Documents;

public sealed record Issuer(
    string Ruc,
    string BusinessName,
    string CommercialName,
    string Address);
