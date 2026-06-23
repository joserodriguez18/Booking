using Booking.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registra todos los IRequestHandler<,> del ensamblado
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Registra todos los AbstractValidator<T> del ensamblado
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Pipeline de validación automática: se ejecuta antes de cada handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
