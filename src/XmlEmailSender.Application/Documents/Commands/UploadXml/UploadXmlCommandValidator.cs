using FluentValidation;

namespace XmlEmailSender.Application.Documents.Commands.UploadXml;

internal sealed class UploadXmlCommandValidator : AbstractValidator<UploadXmlCommand>
{
    public UploadXmlCommandValidator()
    {
        RuleFor(c => c.XmlContent)
            .NotEmpty().WithMessage("El XML es obligatorio.")
            .MinimumLength(50).WithMessage("El XML parece truncado o vacío.");
    }
}
