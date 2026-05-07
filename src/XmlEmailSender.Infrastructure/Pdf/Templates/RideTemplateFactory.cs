using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Infrastructure.Pdf.Templates;

/// <summary>
/// Despacha la plantilla correcta según <see cref="DocumentType"/>.
/// Patrón Factory + Strategy: agregar un nuevo tipo es solo registrar
/// un IRideTemplate más en DI; no se toca el orquestador.
/// </summary>
internal sealed class RideTemplateFactory
{
    private readonly Dictionary<DocumentType, IRideTemplate> _templates;

    public RideTemplateFactory(IEnumerable<IRideTemplate> templates)
    {
        _templates = templates.ToDictionary(t => t.DocumentType, t => t);
    }

    public IRideTemplate For(DocumentType type)
    {
        if (!_templates.TryGetValue(type, out var template))
            throw new NotSupportedException($"No hay plantilla RIDE registrada para {type}.");
        return template;
    }
}
