using Booking.Application.Common.DTOs;
using Booking.Domain.Entities;
using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;
using Booking.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Commands.CreateProperty;

public sealed record CreatePropertyCommand(
    string  Name,
    string  Description,
    string  Location,
    decimal PricePerNight,
    string  Currency,
    Guid    OwnerId
) : IRequest<Guid>;

// ── Validador ────────────────────────────────────────────────────────────────

public sealed class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300)
            .WithMessage("El nombre de la propiedad es obligatorio (máx. 300 caracteres).");
        RuleFor(x => x.Location).NotEmpty().MaximumLength(500)
            .WithMessage("La ubicación es obligatoria.");
        RuleFor(x => x.PricePerNight).GreaterThan(0)
            .WithMessage("El precio por noche debe ser mayor a cero.");
        RuleFor(x => x.OwnerId).NotEmpty()
            .WithMessage("El propietario es obligatorio.");
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Guid>
{
    private readonly IApplicationDbContext _ctx;

    public CreatePropertyCommandHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<Guid> Handle(CreatePropertyCommand req, CancellationToken ct)
    {
        var propietario = await _ctx.Usuarios.FindAsync([req.OwnerId], ct)
            ?? throw new NotFoundException("Usuario", req.OwnerId);

        var precio     = Money.Of(req.PricePerNight, req.Currency);
        var propiedad  = Property.Create(req.Name, req.Description, req.Location, precio, req.OwnerId);

        _ctx.Propiedades.Add(propiedad);
        await _ctx.SaveChangesAsync(ct);

        return propiedad.Id;
    }
}
