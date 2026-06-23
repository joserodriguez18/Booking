# Booking Platform

Plataforma de rentas cortas con validación de identidad por IA (KYC), gestión de disponibilidad estricta y dashboard de rendimiento para propietarios.

## Requisitos Previos

- Docker >= 24 y Docker Compose >= 2.20
- (Opcional para desarrollo local) .NET SDK 10

## Levantar el entorno

```bash
# 1. Copiar variables de entorno y completar con valores reales
cp .env.example .env

# 2. Levantar todos los servicios
docker compose up --build

# 3. (Primera vez) Aplicar migraciones desde el contenedor
docker compose exec api dotnet ef database update
```

| Servicio       | URL                        |
|----------------|----------------------------|
| API + Swagger  | http://localhost:8080/swagger |
| MinIO Console  | http://localhost:9001      |
| PostgreSQL     | localhost:5432             |

Para detener: `docker compose down`  
Para borrar volúmenes: `docker compose down -v`

## Arquitectura

El núcleo está construido sobre **Clean Architecture** con un **Monolito Modular**:

```
Booking.Domain          → Entidades, Value Objects, reglas de negocio puras (sin dependencias externas)
Booking.Application     → Casos de uso (CQRS con MediatR), interfaces de repositorios, DTOs
Booking.Infrastructure  → EF Core + PostgreSQL, MinIO (KYC), MailKit (SMTP), Gemini AI, ClosedXML
Booking.WebAPI          → Controllers, middleware JWT, configuración DI, Dockerfile
```

**Flujo de dependencias:** WebAPI → Infrastructure → Application → Domain

### Decisiones Técnicas

| Problema | Solución adoptada |
|----------|-------------------|
| Double-booking | Constraint único en BD + bloqueo pesimista en la transacción de creación de reserva |
| KYC con IA | Imagen de cédula → MinIO (cifrado SSE-S3) → Gemini Vision API → veredicto → borrado del objeto |
| Autenticación diferida | Endpoints de búsqueda/catálogo son públicos; JWT requerido solo en reserva, wishlist y perfil |
| Horarios fijos | Dominio fuerza check-in 14:00 / check-out 12:00 en el constructor de la entidad `Reservation` |
| Exportación Excel | ClosedXML genera el `.xlsx` en memoria; stream directo al response sin disco temporal |
| Notificaciones | Interfaz `INotificationService` con implementación SMTP (MailKit) + in-app via tabla `Notifications` |

## Desarrollo Local (sin Docker)

```bash
dotnet run --project src/Booking.WebAPI
dotnet test
dotnet test --filter "FullyQualifiedName~NombreDelTest"
dotnet ef migrations add <NombreMigracion> --project src/Booking.Infrastructure --startup-project src/Booking.WebAPI
dotnet ef database update --project src/Booking.Infrastructure --startup-project src/Booking.WebAPI
```
