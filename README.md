# Booking Platform

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
WebAPI → Infrastructure → Application → Domain
```

Cada capa solo puede conocer a la que está a su derecha, nunca al revés.

### Cómo se resolvieron los problemas clave

| Problema | Solución |
|----------|----------|
| **Evitar reservas duplicadas** | Al crear o confirmar una reserva, el sistema consulta todas las reservas confirmadas de esa propiedad y verifica que las fechas no se superpongan. Si hay conflicto, se rechaza con un mensaje claro. |
| **Horarios estandarizados** | El objeto `BookingDateRange` fuerza automáticamente la hora de check-in a las 14:00 y check-out a las 12:00, sin importar lo que envíe el cliente. |
| **Verificación de identidad con IA** | El usuario sube su cédula → se guarda temporalmente en MinIO (almacenamiento cifrado) → Gemini Vision extrae nombre, número de documento y fecha de nacimiento → se emite el veredicto → el documento se elimina de forma permanente. Si Gemini no está disponible, se usa un modo de demostración automáticamente. |
| **Seguridad de documentos** | Los documentos de identidad se borran criptográficamente de MinIO inmediatamente después de la verificación, cumpliendo con requisitos de privacidad. |
| **Fotos públicas vs. privadas** | Las fotos de propiedades están en un bucket público (acceso directo por URL). Los documentos KYC están en un bucket privado con URLs de acceso temporal. |
| **Exportación de reportes** | ClosedXML genera el archivo `.xlsx` completamente en memoria y lo envía directamente al navegador, sin guardar archivos temporales en el servidor. |
| **Tokens seguros** | JWT firmado con HMAC-SHA256 para autenticación. Los refresh tokens se almacenan como hash SHA-256 en la base de datos, nunca el valor original. |

### Tecnologías utilizadas

| Tecnología | Uso |
|------------|-----|
| **.NET 10 + ASP.NET Core** | API REST principal |
| **PostgreSQL** | Base de datos relacional |
| **Entity Framework Core 10** | ORM para acceso a datos |
| **MediatR** | Patrón CQRS (separación de comandos y consultas) |
| **MinIO** | Almacenamiento de archivos (compatible con S3) |
| **Google Gemini Vision** | Extracción de datos de documentos de identidad |
| **MailKit** | Envío de correos electrónicos |
| **ClosedXML** | Generación de archivos Excel |
| **FluentValidation** | Validación de entradas |
| **Docker + Docker Compose** | Contenerización y orquestación |
| **Nginx** | Servidor del frontend estático |
| **Tailwind CSS** | Estilos del frontend |
