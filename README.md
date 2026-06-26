# Booking Platform

> Repositorio: [https://github.com/joserodriguez18/Booking](https://github.com/joserodriguez18/Booking)

Plataforma de rentas cortas que conecta huéspedes y propietarios. Incluye búsqueda de inmuebles, reservas con prevención de conflictos de fechas, lista de favoritos, verificación de identidad mediante Inteligencia Artificial (KYC) y un panel de control con métricas para propietarios.

---

## ¿Qué hace esta plataforma?

| Para el huésped (cliente) | Para el propietario |
|---------------------------|---------------------|
| Explorar propiedades sin registrarse | Publicar y editar sus inmuebles |
| Filtrar por ciudad y fechas | Subir fotos (varias a la vez) |
| Guardar favoritos (wishlist) | Ver métricas: ingresos, ocupación, reservas |
| Reservar con check-in fijo a las 2:00 PM y check-out a las 12:00 PM | Exportar reportes en Excel (.xlsx) |
| Verificar su identidad con foto de cédula (IA) | Recibir notificaciones de nuevas reservas |

---

## Requisitos previos

Solo necesitas tener instalado **Docker Desktop** en tu computador.

- **Windows / Mac:** descárgalo desde [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)
- **Linux:** instala Docker Engine + Docker Compose siguiendo [https://docs.docker.com/engine/install](https://docs.docker.com/engine/install)

> No necesitas instalar .NET, Node.js, PostgreSQL ni ninguna otra herramienta. Docker se encarga de todo.

---

## Instalación paso a paso

### 1. Descarga el proyecto

Si tienes Git instalado, abre una terminal y ejecuta:

```bash
git clone https://github.com/joserodriguez18/Booking.git
cd Booking
```

Si no tienes Git, descarga el proyecto como archivo ZIP desde GitHub y descomprímelo.

### 2. Crea el archivo de configuración

Dentro de la carpeta del proyecto encontrarás un archivo llamado `.env.example`. Cópialo y renómbralo a `.env`:

**En Windows (PowerShell):**
```powershell
Copy-Item .env.example .env
```

**En Mac / Linux:**
```bash
cp .env.example .env
```

Abre el archivo `.env` con cualquier editor de texto (Bloc de notas, VS Code, etc.). Los valores predeterminados ya funcionan para desarrollo local. Si tienes una clave de la API de Gemini (Google AI), puedes pegarla en el campo `GEMINI_API_KEY`; si no, la plataforma usará un modo de demostración automáticamente.

### 3. Levanta todos los servicios

En la terminal, dentro de la carpeta del proyecto, ejecuta:

```bash
docker compose up --build -d
```

Este comando descarga las imágenes necesarias, construye la aplicación y arranca todos los servicios. **La primera vez puede tardar entre 3 y 10 minutos** dependiendo de tu conexión a internet.

### 4. Listo — abre la aplicación

Una vez que el comando termine, abre tu navegador y ve a:

| ¿Qué quieres abrir? | URL |
|---------------------|-----|
| **Aplicación web** (aquí empieza todo) | [http://localhost:3000](http://localhost:3000) |
| Documentación técnica de la API (Swagger) | [http://localhost:8081/swagger](http://localhost:8081/swagger) |
| Consola de almacenamiento de archivos (MinIO) | [http://localhost:9001](http://localhost:9001) |

---

## Cómo usar la plataforma

### Registrarse

1. Ve a `http://localhost:3000` y haz clic en **Ingresar / Registrarse**.
2. Elige tu rol:
   - **"Quiero reservar"** → eres Huésped. Puedes explorar, guardar favoritos y hacer reservas.
   - **"Quiero publicar"** → eres Propietario. Puedes gestionar inmuebles y ver tu dashboard.
3. Completa el formulario y haz clic en **Crear cuenta**.

### Como huésped

- Navega el catálogo desde la página de inicio.
- Usa el buscador para filtrar por ciudad y fechas.
- Haz clic en el corazón ❤️ para guardar un inmueble en favoritos.
- Para hacer una reserva, selecciona las fechas y confirma.
- Antes de confirmar tu primera reserva, el sistema te pedirá verificar tu identidad: sube una foto de tu cédula (puedes subir cara frontal y trasera).

### Como propietario

- Accede al **Panel de control** desde el menú superior.
- Crea un inmueble con el botón **+ Nueva propiedad**.
- Sube fotos de la propiedad (puedes subir varias a la vez).
- Consulta tus métricas de ingresos, tasa de ocupación y reservas.
- Descarga reportes en Excel con el botón **Exportar**.

---

## Detener la aplicación

```bash
docker compose down
```

Para borrar todos los datos almacenados (base de datos, fotos, etc.) y empezar desde cero:

```bash
docker compose down -v
```

---

## Estructura del proyecto

```
Booking/
├── src/
│   ├── Booking.Domain/          # Reglas de negocio puras (sin dependencias externas)
│   │   ├── Entities/            # Objetos principales: Usuario, Propiedad, Reserva, Documento
│   │   ├── ValueObjects/        # Tipos de valor: Dinero (Money), Rango de fechas
│   │   └── Exceptions/          # Errores de dominio (ej. conflicto de fechas)
│   │
│   ├── Booking.Application/     # Casos de uso: qué puede hacer el sistema
│   │   ├── Auth/                # Registro, login, refresh de token
│   │   ├── Bookings/            # Crear, confirmar y cancelar reservas
│   │   ├── Properties/          # Crear, editar y gestionar fotos de propiedades
│   │   ├── KYC/                 # Verificación de identidad con IA
│   │   ├── Wishlist/            # Lista de favoritos
│   │   └── Owner/               # Dashboard y exportación de reportes
│   │
│   ├── Booking.Infrastructure/  # Implementaciones técnicas
│   │   ├── Persistence/         # Base de datos PostgreSQL con EF Core
│   │   ├── Services/
│   │   │   ├── AI/              # Integración con Gemini Vision (KYC)
│   │   │   ├── Storage/         # MinIO: fotos de propiedades y documentos KYC
│   │   │   ├── Auth/            # JWT y refresh tokens
│   │   │   └── Email/           # Notificaciones por correo (MailKit)
│   │   └── Reports/             # Generación de Excel con ClosedXML
│   │
│   └── Booking.WebAPI/          # API REST: endpoints, autenticación, middleware
│
├── frontend/                    # Interfaz web
│   ├── index.html               # Página principal (catálogo de propiedades)
│   ├── auth.html                # Login y registro
│   ├── property.html            # Detalle de una propiedad y formulario de reserva
│   ├── profile.html             # Perfil del usuario: reservas, favoritos, KYC
│   ├── dashboard.html           # Panel del propietario (solo para rol Owner)
│   └── js/api.js                # Cliente HTTP que conecta el frontend con la API
│
├── docker-compose.yml           # Orquestación de todos los servicios
├── .env.example                 # Plantilla de variables de entorno
└── README.md                    # Este archivo
```

---

## Arquitectura y decisiones técnicas

La aplicación está construida sobre **Clean Architecture**, un patrón de diseño que separa el código en capas con reglas claras de dependencia. La idea central es que las reglas de negocio no dependen de ninguna tecnología específica (base de datos, framework, IA), lo que facilita cambiar un componente sin afectar a los demás.

```
┌──────────────────────────────────────────────┐
│           WebAPI  (Presentación)             │  ← capa más externa
│  Controllers · Middleware · JWT · Swagger    │
├──────────────────────────────────────────────┤
│        Infrastructure  (Infraestructura)     │
│  EF Core · PostgreSQL · MinIO · Gemini       │
│          SMTP · BCrypt · JWT service         │
├──────────────────────────────────────────────┤
│         Application  (Casos de Uso)          │
│  Commands · Queries · Validadores · DTOs     │
├──────────────────────────────────────────────┤
│            Domain  (Núcleo)                  │  ← capa más interna
│  Entidades · Value Objects · Excepciones     │
└──────────────────────────────────────────────┘
```

**Regla de oro:** las dependencias solo apuntan hacia adentro. El Domain no importa nada de infraestructura. Si mañana cambia la base de datos, el proveedor de IA o el servidor de email, **solo cambia Infrastructure** — el dominio y los casos de uso no se tocan.

---

### Domain-Driven Design (DDD)

DDD no es solo una forma de organizar carpetas: es la decisión de que el **código hable el idioma del negocio**. En lugar de tener un servicio con 20 métodos que manipulan datos, cada entidad del dominio conoce sus propias reglas y las hace cumplir internamente.

#### Entidades Ricas

Las entidades no son simples contenedores de datos (como sería en un modelo anémico). Tienen constructores privados y factory methods que garantizan que un objeto solo puede existir en un estado válido.

**Ejemplo: crear una reserva**

```csharp
// ❌ Incorrecto — nada impide crear una reserva inválida
var reserva = new Booking { PropertyId = id, CheckIn = ayer };

// ✅ Correcto — el dominio valida todo antes de permitir la creación
var reserva = Booking.Create(propertyId, guestId, checkIn, checkOut,
    precioNoche, reservasExistentes);
// Si hay conflicto de fechas → lanza BookingConflictException
// Si las fechas son inválidas → lanza DomainException
```

El método `Booking.Create()` recibe todas las reservas confirmadas de esa propiedad y verifica internamente que no haya solapamiento. El **dominio mismo** previene el double-booking, no un servicio externo.

**Entidades del sistema:**

| Entidad | Reglas de negocio encapsuladas |
|---|---|
| `User` | Controla su propio ciclo KYC: `NotStarted → Pending → Approved / Rejected` |
| `Property` | Límite de 10 fotos por propiedad; solo el dueño puede modificarla |
| `Booking` | Previene double-booking; transiciones de estado `Pending → Confirmed → Cancelled` con validaciones en cada paso |
| `IdentityDocument` | Almacena datos extraídos por la IA ligados al usuario |
| `Notification` | Notificaciones in-app tipadas por evento |
| `WishlistItem` | Vínculo entre un usuario y una propiedad guardada |
| `RefreshToken` | Token de renovación de sesión con fecha de expiración |

#### Value Objects

Los Value Objects son conceptos del negocio que se identifican por su **valor**, no por un ID. Son inmutables y no tienen efectos secundarios.

**`BookingDateRange`** — el invariante más crítico del sistema:
- Recibe fechas simples (`DateOnly`) y fuerza internamente el check-in a las **14:00 UTC** y el check-out a las **12:00 UTC**
- Implementa `OverlapsWith()` con lógica de intervalos abiertos para detectar conflictos
- No se puede construir con fechas inválidas — si `checkOut ≤ checkIn`, lanza excepción en el constructor

**`Money`** — monto con moneda:
- No permite montos negativos
- Soporta multiplicación (`precioNoche * cantidadNoches`) para calcular totales
- Definido como `sealed record`, lo que garantiza igualdad por valor de forma automática

#### Excepciones de Dominio

El dominio lanza excepciones tipadas que el middleware global intercepta y convierte a respuestas HTTP uniformes. No hay try/catch en los controllers.

| Excepción | Cuándo | HTTP |
|---|---|---|
| `BookingConflictException` | Fechas solapadas en la misma propiedad | 409 Conflict |
| `NotFoundException` | Entidad no existe en base de datos | 404 Not Found |
| `DomainException` | Cualquier regla de negocio violada | 400 Bad Request |

---

### CQRS con MediatR

**CQRS (Command Query Responsibility Segregation)** separa las operaciones que modifican estado (Commands) de las que solo leen (Queries). MediatR actúa como el bus que enruta cada mensaje a su handler.

```
Request HTTP → Controller → mediator.Send(Command) → Handler → Respuesta
```

Cada caso de uso vive en su propio archivo. No hay servicios "dios" con 20 métodos. Agregar una funcionalidad nueva es agregar un archivo nuevo, sin tocar nada existente.

**Casos de uso implementados:**

```
Auth/          Login · Register · RefreshToken
Bookings/      CreateBooking · ConfirmBooking · CancelBooking · GetMyBookings
Properties/    CreateProperty · UpdateProperty · UploadPhoto · DeletePhoto · GetProperties · GetById
KYC/           UploadIdentityDocument
Wishlist/      AddToWishlist · RemoveFromWishlist · GetWishlist
Owner/         GetDashboard · ExportBookingsReport
```

**Pipeline de validación automática:** antes de que cualquier Command llegue a su Handler, pasa por `ValidationBehavior`. Este ejecuta el `AbstractValidator<T>` correspondiente (FluentValidation). Si la validación falla, la excepción se lanza antes de abrir una conexión a la base de datos.

---

### Cómo se resolvieron los problemas clave

| Problema | Solución |
|----------|----------|
| **Evitar reservas duplicadas** | `Booking.Create()` y `booking.Confirm()` reciben todas las reservas confirmadas de la propiedad y verifican solapamiento en el dominio. Si hay conflicto, lanza `BookingConflictException` (HTTP 409). |
| **Horarios estandarizados** | `BookingDateRange.Create()` aplica automáticamente check-in 14:00 y check-out 12:00 UTC. El cliente solo envía fechas simples; el sistema impone las horas. |
| **Verificación de identidad con IA** | Usuario sube cédula → MinIO la almacena → Gemini Vision extrae nombre, número y fecha de nacimiento → se emite el veredicto → el documento se elimina permanentemente. Ante error 429 (rate limit), reintenta 3 veces con espera exponencial. |
| **Seguridad de documentos** | Los documentos KYC se eliminan de MinIO inmediatamente después de la verificación. Solo quedan los datos extraídos (número de documento, fecha de nacimiento) en la tabla de usuarios. |
| **Modo desarrollo sin API real** | Si `GEMINI_API_KEY` es el placeholder del `.env.example`, el `KycService` devuelve datos mock coherentes deterministas sin llamar a ninguna API externa. |
| **Exportación de reportes** | ClosedXML genera el `.xlsx` en memoria y lo envía como stream directamente al navegador. No se escriben archivos temporales en el servidor. |
| **Tokens seguros** | JWT con HMAC-SHA256 (acceso, 60 min). Refresh tokens almacenados como hash SHA-256 en base de datos; nunca el valor original. |
| **Emails no bloqueantes** | Los envíos de correo son fire-and-forget (`_ = email.SendAsync(...).ConfigureAwait(false)`). Un servidor SMTP lento no retrasa la respuesta HTTP al cliente. |

---

### Por qué es escalable

#### A nivel de código

| Decisión | Consecuencia a futuro |
|---|---|
| **Clean Architecture** | Cambiar PostgreSQL por otra BD, Gemini por OpenAI, o MinIO por S3 solo requiere reimplementar una interfaz en Infrastructure |
| **CQRS** | Las Queries (lecturas) se pueden optimizar independientemente: agregar caché de Redis, read replicas, o projections sin tocar los Commands |
| **Sin repositorio genérico** | Los handlers usan EF Core directamente a través de `IApplicationDbContext`, aprovechando toda la potencia de LINQ para queries complejos sin abstracciones innecesarias |
| **Módulos independientes** | Cada módulo (KYC, Wishlist, Owner, Bookings) puede extraerse como microservicio sin romper los demás |

#### A nivel de infraestructura

| Componente | Por qué escala |
|---|---|
| **PostgreSQL** | Soporta millones de registros; se puede agregar read replica para el catálogo público de propiedades sin cambiar código |
| **MinIO** | API 100% compatible con S3 de AWS. Migrar a producción en la nube es cambiar las credenciales en `.env`, no reescribir código |
| **JWT stateless** | No hay sesiones en el servidor. Se pueden agregar instancias del API sin coordinación entre ellas |
| **Docker → Kubernetes** | La containerización hace trivial escalar el API horizontalmente detrás de un balanceador de carga |
| **Retry automático en EF Core** | `EnableRetryOnFailure` maneja fallos transitorios de red entre contenedores sin intervención manual |

#### Funcionalidades que se pueden agregar sin romper lo existente

- **Pagos:** nuevo módulo `Payments/Commands/ProcessPayment` — no toca ningún handler existente
- **Caché del catálogo:** se agrega en el handler `GetPropertiesQuery`, transparente para el resto del sistema
- **Eventos de dominio:** los factory methods de las entidades son el punto natural para emitirlos
- **Multi-idioma:** los mensajes están centralizados en las excepciones de dominio

---

### Tecnologías utilizadas

| Tecnología | Uso |
|------------|-----|
| **.NET 10 + ASP.NET Core** | API REST principal |
| **PostgreSQL 16** | Base de datos relacional |
| **Entity Framework Core 10** | ORM y migraciones |
| **MediatR** | Bus de mensajes para CQRS |
| **FluentValidation** | Validación declarativa de commands/queries |
| **MinIO** | Almacenamiento de objetos compatible con S3 |
| **Google Gemini Vision** | Extracción de datos de documentos de identidad (KYC) |
| **MailKit** | Envío de correos electrónicos vía SMTP |
| **ClosedXML** | Generación de archivos Excel en memoria |
| **BCrypt.Net** | Hash seguro de contraseñas |
| **DotNetEnv** | Carga de variables desde `.env` en desarrollo local |
| **Docker + Docker Compose** | Containerización y orquestación de servicios |
| **Nginx** | Servidor del frontend estático |
| **Tailwind CSS** | Estilos del frontend |
