# Booking Platform

Plataforma de rentas cortas con validación de identidad por IA (KYC), gestión de disponibilidad estricta y dashboard de rendimiento para propietarios.

## Requisitos Previos

- Docker >= 24 y Docker Compose >= 2.20
- (Opcional para desarrollo local) .NET SDK 10

## Levantar el entorno

```bash
# 1. Copiar variables de entorno y completar con valores reales
cp .env.example .env

# 2. Levantar todos los servicios (API, PostgreSQL, MinIO, Frontend)
docker compose up --build -d
```

Las migraciones de base de datos se aplican automáticamente al iniciar el contenedor `api`.

| Servicio          | URL                              |
|-------------------|----------------------------------|
| Frontend          | http://localhost:3000            |
| API + Swagger     | http://localhost:8081/swagger    |
| MinIO Console     | http://localhost:9001            |
| PostgreSQL        | localhost:5433                   |

Para detener: `docker compose down`  
Para borrar volúmenes: `docker compose down -v`

## Roles de usuario

| Rol | Capacidades |
|-----|-------------|
| **Guest** (Huésped) | Explorar propiedades, reservar, gestionar favoritos (wishlist), verificación KYC |
| **Owner** (Propietario) | Publicar y editar propiedades, subir fotos, dashboard de rendimiento, exportar Excel |

Al registrarse se elige el rol. El dashboard de propietario es exclusivo para Owner.

## Arquitectura

Núcleo en **Clean Architecture** con **Monolito Modular**:

```
Booking.Domain          → Entidades, Value Objects, reglas de negocio puras (sin dependencias externas)
Booking.Application     → Casos de uso (CQRS con MediatR), interfaces, DTOs
Booking.Infrastructure  → EF Core + PostgreSQL, MinIO (KYC y fotos), MailKit (SMTP), Gemini AI, ClosedXML
Booking.WebAPI          → Controllers, middleware JWT, configuración DI, Dockerfile
frontend/               → HTML + Tailwind CSS + JS vanilla, servido por Nginx
```

**Flujo de dependencias:** WebAPI → Infrastructure → Application → Domain

### Decisiones Técnicas

| Problema | Solución adoptada |
|----------|-------------------|
| Double-booking | Constraint único en BD + bloqueo pesimista en la transacción de creación de reserva |
| KYC con IA | Imagen de cédula (frente + reverso) → MinIO → Gemini Vision API → veredicto → borrado criptográfico |
| KYC mock en desarrollo | Si `GEMINI_API_KEY` no está configurada o Gemini retorna 429, se usa un mock determinista por documento |
| Autenticación diferida | Endpoints de búsqueda/catálogo son públicos; JWT requerido solo en reserva, wishlist y perfil |
| Horarios fijos | Dominio fuerza check-in 14:00 / check-out 12:00 en el constructor de `BookingDateRange` |
| Exportación Excel | ClosedXML genera el `.xlsx` en memoria; stream directo al response sin disco temporal |
| Notificaciones | In-app via tabla `notificaciones` + alertas por email (MailKit/SMTP) en eventos clave |
| Fotos públicas | Bucket `property-photos` con política S3 de lectura pública; bucket `kyc-documents` privado con URLs presignadas |
| Seguridad de documentos | Documentos KYC se eliminan de MinIO inmediatamente tras la verificación (borrado criptográfico) |
| Tokens | JWT (HMAC-SHA256) + refresh token rotativo almacenado como hash SHA-256 en BD |

## Desarrollo Local (sin Docker)

```bash
dotnet run --project src/Booking.WebAPI
dotnet test
dotnet test --filter "FullyQualifiedName~NombreDelTest"
dotnet ef migrations add <NombreMigracion> --project src/Booking.Infrastructure --startup-project src/Booking.WebAPI
dotnet ef database update --project src/Booking.Infrastructure --startup-project src/Booking.WebAPI
```
