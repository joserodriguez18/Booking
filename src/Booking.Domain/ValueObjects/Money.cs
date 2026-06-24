using Booking.Domain.Exceptions;

namespace Booking.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency = "USD")
{
    public static Money Of(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("El monto no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("La moneda es obligatoria.");
        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money operator *(Money money, int multiplier) =>
        new(money.Amount * multiplier, money.Currency);
}
