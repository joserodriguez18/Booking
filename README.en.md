# Booking Platform

A short-term rental platform that connects guests and property owners. It includes property search, conflict-free date booking, wishlists, AI-powered identity verification (KYC), and a performance dashboard with metrics for owners.

---

## What does this platform do?

| For the guest (tenant) | For the owner (host) |
|------------------------|----------------------|
| Browse properties without signing up | Publish and edit listings |
| Filter by city and dates | Upload photos (multiple at once) |
| Save favorites (wishlist) | View metrics: revenue, occupancy rate, bookings |
| Book with fixed check-in at 2:00 PM and check-out at 12:00 PM | Export reports as Excel (.xlsx) files |
| Verify identity with an ID photo (AI-powered) | Receive notifications for new bookings |

---

## Prerequisites

All you need is **Docker Desktop** installed on your computer.

- **Windows / Mac:** download it from [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)
- **Linux:** install Docker Engine + Docker Compose by following [https://docs.docker.com/engine/install](https://docs.docker.com/engine/install)

> You do not need to install .NET, Node.js, PostgreSQL, or any other tool. Docker handles everything.

---

## Step-by-step installation

### 1. Download the project

If you have Git installed, open a terminal and run:

```bash
git clone https://github.com/joserodriguez18/Booking.git
cd Booking
```

If you don't have Git, download the project as a ZIP file from GitHub and extract it.

### 2. Create the configuration file

Inside the project folder you will find a file called `.env.example`. Copy it and rename it to `.env`:

**On Windows (PowerShell):**
```powershell
Copy-Item .env.example .env
```

**On Mac / Linux:**
```bash
cp .env.example .env
```

Open the `.env` file with any text editor (Notepad, VS Code, etc.). The default values already work for local development. If you have a Gemini API key (Google AI), you can paste it in the `GEMINI_API_KEY` field; otherwise the platform will automatically use a demo mode.

### 3. Start all services

In the terminal, inside the project folder, run:

```bash
docker compose up --build -d
```

This command downloads the required images, builds the application, and starts all services. **The first time this may take 3 to 10 minutes** depending on your internet connection.

### 4. Done — open the application

Once the command finishes, open your browser and go to:

| What do you want to open? | URL |
|---------------------------|-----|
| **Web application** (start here) | [http://localhost:3000](http://localhost:3000) |
| API technical documentation (Swagger) | [http://localhost:8081/swagger](http://localhost:8081/swagger) |
| File storage console (MinIO) | [http://localhost:9001](http://localhost:9001) |

---

## How to use the platform

### Sign up

1. Go to `http://localhost:3000` and click **Sign In / Sign Up**.
2. Choose your role:
   - **"I want to book"** → you are a Guest. You can browse, save favorites, and make reservations.
   - **"I want to list"** → you are an Owner. You can manage properties and view your dashboard.
3. Fill in the form and click **Create account**.

### As a guest

- Browse the catalog from the home page.
- Use the search bar to filter by city and dates.
- Click the heart ❤️ icon to save a property to your wishlist.
- To make a booking, select dates and confirm.
- Before confirming your first booking, the system will ask you to verify your identity: upload a photo of your ID (you can upload both front and back).

### As an owner

- Access the **Dashboard** from the top menu.
- Create a property with the **+ New property** button.
- Upload property photos (you can upload several at once).
- Check your revenue metrics, occupancy rate, and booking history.
- Download Excel reports with the **Export** button.

---

## Stopping the application

```bash
docker compose down
```

To delete all stored data (database, photos, etc.) and start fresh:

```bash
docker compose down -v
```

---

## Project structure

```
Booking/
├── src/
│   ├── Booking.Domain/          # Pure business rules (no external dependencies)
│   │   ├── Entities/            # Core objects: User, Property, Booking, Document
│   │   ├── ValueObjects/        # Value types: Money, BookingDateRange
│   │   └── Exceptions/          # Domain errors (e.g. date conflict)
│   │
│   ├── Booking.Application/     # Use cases: what the system can do
│   │   ├── Auth/                # Register, login, token refresh
│   │   ├── Bookings/            # Create, confirm, and cancel bookings
│   │   ├── Properties/          # Create, edit, and manage property photos
│   │   ├── KYC/                 # AI-powered identity verification
│   │   ├── Wishlist/            # Favorites list
│   │   └── Owner/               # Dashboard and report exports
│   │
│   ├── Booking.Infrastructure/  # Technical implementations
│   │   ├── Persistence/         # PostgreSQL database with EF Core
│   │   ├── Services/
│   │   │   ├── AI/              # Gemini Vision integration (KYC)
│   │   │   ├── Storage/         # MinIO: property photos and KYC documents
│   │   │   ├── Auth/            # JWT and refresh tokens
│   │   │   └── Email/           # Email notifications (MailKit)
│   │   └── Reports/             # Excel generation with ClosedXML
│   │
│   └── Booking.WebAPI/          # REST API: endpoints, authentication, middleware
│
├── frontend/                    # Web interface
│   ├── index.html               # Home page (property catalog)
│   ├── auth.html                # Login and registration
│   ├── property.html            # Property detail and booking form
│   ├── profile.html             # User profile: bookings, wishlist, KYC
│   ├── dashboard.html           # Owner dashboard (Owner role only)
│   └── js/api.js                # HTTP client connecting the frontend to the API
│
├── docker-compose.yml           # All-services orchestration
├── .env.example                 # Environment variables template
└── README.md                    # Main readme (Spanish)
```

---

## Architecture and technical decisions

The application is built on **Clean Architecture**, a design pattern that separates code into layers with strict dependency rules. The core idea is that business rules do not depend on any specific technology (database, framework, AI), making it easy to swap one component without affecting others.

```
WebAPI → Infrastructure → Application → Domain
```

Each layer can only reference the one to its right, never the other way around.

### How key problems were solved

| Problem | Solution |
|---------|----------|
| **Preventing double-bookings** | When creating or confirming a booking, the system fetches all confirmed bookings for that property and checks for date overlaps. If there is a conflict, the request is rejected with a clear message. |
| **Standardized check-in/out times** | The `BookingDateRange` value object automatically enforces check-in at 14:00 and check-out at 12:00, regardless of what the client sends. |
| **AI identity verification** | The user uploads their ID → it is temporarily stored in MinIO (encrypted storage) → Gemini Vision extracts name, document number, and birth date → a verdict is issued → the document is permanently deleted. If Gemini is unavailable, a demo mode is used automatically. |
| **Document security** | Identity documents are cryptographically deleted from MinIO immediately after verification, meeting privacy requirements. |
| **Public vs. private photos** | Property photos are in a public bucket (direct URL access). KYC documents are in a private bucket with short-lived presigned URLs. |
| **Report exports** | ClosedXML generates the `.xlsx` file entirely in memory and streams it directly to the browser, with no temporary files saved on the server. |
| **Secure tokens** | HMAC-SHA256 signed JWT for authentication. Refresh tokens are stored as SHA-256 hashes in the database — never the raw value. |

### Technologies used

| Technology | Purpose |
|------------|---------|
| **.NET 10 + ASP.NET Core** | Main REST API |
| **PostgreSQL** | Relational database |
| **Entity Framework Core 10** | ORM for data access |
| **MediatR** | CQRS pattern (command/query separation) |
| **MinIO** | File storage (S3-compatible) |
| **Google Gemini Vision** | Data extraction from identity documents |
| **MailKit** | Email sending |
| **ClosedXML** | Excel file generation |
| **FluentValidation** | Input validation |
| **Docker + Docker Compose** | Containerization and orchestration |
| **Nginx** | Static frontend server |
| **Tailwind CSS** | Frontend styles |
