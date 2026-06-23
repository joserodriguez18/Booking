using FluentValidation;
using MediatR;

namespace Booking.Application.Common.Behaviors;

/// <summary>
/// Pipeline de MediatR que ejecuta automáticamente todos los validadores
/// FluentValidation registrados para el comando/query antes de que llegue al handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var contexto = new ValidationContext<TRequest>(request);

        var errores = _validators
            .Select(v => v.Validate(contexto))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (errores.Count != 0)
            throw new ValidationException(errores);

        return await next(cancellationToken);
    }
}
