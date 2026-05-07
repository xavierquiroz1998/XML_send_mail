using FluentValidation;

namespace XmlEmailSender.Application.Smtp.Commands.SaveSmtpConfiguration;

internal sealed class SaveSmtpConfigurationCommandValidator
    : AbstractValidator<SaveSmtpConfigurationCommand>
{
    public SaveSmtpConfigurationCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Host).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Port).InclusiveBetween(1, 65535);
        RuleFor(c => c.Username).NotEmpty().MaximumLength(255);
        RuleFor(c => c.FromEmail).NotEmpty().EmailAddress();
        RuleFor(c => c.FromName).NotEmpty().MaximumLength(100);

        // En creación la password es obligatoria; al actualizar puede venir null
        // (en cuyo caso conservamos la existente).
        When(c => c.Id is null, () =>
        {
            RuleFor(c => c.NewPassword).NotEmpty().WithMessage("La contraseña es obligatoria al crear.");
        });
    }
}
