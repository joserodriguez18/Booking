namespace Booking.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entidad, object id)
        : base($"{entidad} con ID '{id}' no fue encontrado.") { }
}
