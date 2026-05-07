using FluentValidation;

namespace XmlEmailSender.Application.Documents.Commands.SendDocumentEmail;

internal sealed class SendDocumentEmailCommandValidator : AbstractValidator<SendDocumentEmailCommand>
{
    public SendDocumentEmailCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        When(c => !string.IsNullOrEmpty(c.RecipientOverride), () =>
        {
            RuleFor(c => c.RecipientOverride!)
                .EmailAddress().WithMessage("El correo de destino es inválido.");
        });
    }
}
