using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Booking.Infrastructure.Persistence;

// Usada exclusivamente por dotnet-ef en tiempo de diseño para generar migraciones
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Cadena de conexión de diseño — solo para generación de migraciones
        var cadenaConexion = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=127.0.0.1;Port=5433;Database=booking_db;Username=postgres;Password=password";

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(cadenaConexion, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(opciones);
    }
}
