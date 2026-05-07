using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using XmlEmailSender.Application.Common.Behaviors;

namespace XmlEmailSender.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Orden importa: Logging envuelve todo (ve éxito/fallo final);
            // luego validación corta antes del UoW para no abrir tx innecesarias;
            // luego UoW; finalmente el handler.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
