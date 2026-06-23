using Booking.Application.Common.DTOs;
using Booking.Domain.Exceptions;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Queries.GetPropertyById;

public sealed record GetPropertyByIdQuery(Guid PropertyId) : IRequest<PropertyDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetPropertyByIdQueryHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDto>
{
    private readonly IApplicationDbContext _ctx;

    public GetPropertyByIdQueryHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<PropertyDto> Handle(GetPropertyByIdQuery req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades
            .Where(p => p.Id == req.PropertyId && p.IsActive)
            .Select(p => new PropertyDto(
                p.Id, p.Name, p.Description, p.Location,
                p.PricePerNight.Amount, p.PricePerNight.Currency,
                p.OwnerId, p.IsActive, p.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        return propiedad;
    }
}
