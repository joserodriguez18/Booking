using Booking.Domain.Enums;
using Booking.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Owner.Queries.ExportReport;

public sealed record ExportBookingsReportCommand(
    Guid  OwnerId,
    Guid? PropertyId  // null = todas las propiedades del propietario
) : IRequest<byte[]>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class ExportBookingsReportCommandHandler : IRequestHandler<ExportBookingsReportCommand, byte[]>
{
    private readonly IApplicationDbContext _ctx;

    public ExportBookingsReportCommandHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<byte[]> Handle(ExportBookingsReportCommand req, CancellationToken ct)
    {
        // Obtiene las propiedades del propietario (filtrando por una si se especifica)
        var propIds = await _ctx.Propiedades
            .Where(p => p.OwnerId == req.OwnerId &&
                        (req.PropertyId == null || p.Id == req.PropertyId))
            .Select(p => p.Id)
            .ToListAsync(ct);

        // Datos del reporte con JOIN para obtener nombres de propiedad y huésped
        var datos = await (
            from r in _ctx.Reservas
            join p in _ctx.Propiedades on r.PropertyId equals p.Id
            join u in _ctx.Usuarios    on r.GuestId    equals u.Id
            where propIds.Contains(r.PropertyId) && r.Status == BookingStatus.Confirmed
            orderby r.DateRange.CheckIn descending
            select new
            {
                ReservaId      = r.Id,
                PropiedadNombre = p.Name,
                HuespedNombre  = u.Name,
                HuespedEmail   = u.Email,
                CheckIn        = r.DateRange.CheckIn,
                CheckOut       = r.DateRange.CheckOut,
                PrecioTotal    = r.TotalPrice.Amount,
                Moneda         = r.TotalPrice.Currency
            }
        ).ToListAsync(ct);

        // Genera el archivo Excel en memoria
        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Reservas");

        // Encabezados con estilo
        string[] encabezados = ["ID Reserva", "Propiedad", "Huésped", "Email Huésped",
                                 "Check-in", "Check-out", "Noches", "Precio Pagado", "Moneda"];
        for (int i = 0; i < encabezados.Length; i++)
        {
            var celda = hoja.Cell(1, i + 1);
            celda.Value = encabezados[i];
            celda.Style.Font.Bold         = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            celda.Style.Font.FontColor    = XLColor.White;
        }

        // Filas de datos
        int fila = 2;
        foreach (var d in datos)
        {
            var noches = (int)(d.CheckOut - d.CheckIn).TotalDays;
            hoja.Cell(fila, 1).Value = d.ReservaId.ToString();
            hoja.Cell(fila, 2).Value = d.PropiedadNombre;
            hoja.Cell(fila, 3).Value = d.HuespedNombre;
            hoja.Cell(fila, 4).Value = d.HuespedEmail;
            hoja.Cell(fila, 5).Value = d.CheckIn.ToString("yyyy-MM-dd HH:mm");
            hoja.Cell(fila, 6).Value = d.CheckOut.ToString("yyyy-MM-dd HH:mm");
            hoja.Cell(fila, 7).Value = noches;
            hoja.Cell(fila, 8).Value = d.PrecioTotal;
            hoja.Cell(fila, 9).Value = d.Moneda;
            hoja.Cell(fila, 8).Style.NumberFormat.Format = "#,##0.00";
            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
