# MAUI Scheduler — Design

Date: 2026-10-08

## Goal

A cross-platform appointment scheduler (Android + Windows) built with .NET 10 MAUI, backed by
SQL Server. Creating an appointment sends an SMS confirmation to the client. Modern visual style
with an optional dark mode.

## Approaches considered

1. **MAUI app talks directly to SQL Server** (Microsoft.Data.SqlClient inside the app).
   Simplest, but it embeds DB and Twilio credentials in a mobile binary, SqlClient has
   poor/unsupported behaviour on Android, and phones usually can't reach a SQL Server on a LAN.
2. **MAUI app + ASP.NET Core API in front of SQL Server** (chosen).
   The API owns the database (EF Core) and SMS sending (Twilio). The app only speaks HTTPS/JSON.
   Credentials stay server-side, Android works, and the API can be hosted anywhere.
3. **MAUI Blazor Hybrid** — same backend split, but web UI inside a WebView. Rejected:
   native MAUI controls give a better-feeling result on both targets for this scope.

## Architecture

```
Scheduler.sln
├── src/Scheduler.Shared   # DTOs + enums shared by API and App (net10.0)
├── src/Scheduler.Api      # ASP.NET Core minimal API, EF Core (SQL Server), Twilio SMS
└── src/Scheduler.App      # .NET MAUI (net10.0-android, net10.0-windows), MVVM
tests/Scheduler.Api.Tests  # xUnit: appointment service + SMS trigger behaviour
```

### Data model (SQL Server, EF Core migrations)

- `Clients` — Id, FirstName, LastName, PhoneNumber (E.164), Email?, Notes?, CreatedAt
- `Appointments` — Id, ClientId (FK), Title, Description?, StartUtc, EndUtc, Location?,
  Status (Scheduled | Completed | Cancelled | NoShow), Color?, CreatedAt, UpdatedAt
- `SmsMessages` — Id, AppointmentId (FK), ToPhoneNumber, Body, Status (Pending|Sent|Failed),
  ProviderMessageId?, Error?, SentAt  — an audit log of every SMS attempt.

### API (Scheduler.Api)

- `GET/POST/PUT/DELETE /api/clients`
- `GET /api/appointments?from=&to=` · `GET /api/appointments/{id}` · `POST` · `PUT` ·
  `DELETE` · `POST /api/appointments/{id}/resend-sms`
- `GET /api/health`
- `AppointmentService` validates (end > start, client exists, no overlapping appointment for
  the same client), persists, then calls `ISmsSender`. SMS failure is logged to `SmsMessages`
  and does **not** fail the booking (the appointment is already saved; the user can resend).
- `ISmsSender` implementations: `TwilioSmsSender` (Twilio REST SDK) and `LoggingSmsSender`
  (dev fallback when Twilio isn't configured — writes to the log and marks the row Sent with a
  `fake:` provider id so the whole flow is testable without an account).
- Configuration: `ConnectionStrings:SchedulerDb`, `Twilio:AccountSid`, `Twilio:AuthToken`,
  `Twilio:FromNumber`. Defaults target `(localdb)\MSSQLLocalDB`. Database is created /
  migrated on startup in Development.

### App (Scheduler.App)

- MVVM with CommunityToolkit.Mvvm; `IApiClient` wraps `HttpClient` (base URL from settings;
  Android emulator default `https://10.0.2.2:7xxx`, Windows `https://localhost:7xxx`).
- Shell with a tab bar: **Schedule** (day strip + grouped appointment cards, pull to refresh),
  **Clients** (list + add/edit), **Settings** (theme: System/Light/Dark, API URL).
- **Appointment editor** page: client picker, title, date, start/end time, location, notes,
  color swatch. Save → POST → toast "Booked — SMS sent to +1…".
- Theme: indigo→violet gradient header, teal/coral accents, 16-px rounded cards, soft shadows.
  All colours defined as light/dark pairs in `Resources/Styles/Colors.xaml` and consumed via
  `AppThemeBinding`. Theme choice persisted with `Preferences` and applied via
  `Application.Current.UserAppTheme`.

### Error handling

- API returns RFC 7807 problem details for validation errors; app surfaces the message inline.
- Network failures in the app show a non-blocking banner and keep the last loaded data.
- SMS errors never block scheduling; status is visible on the appointment detail with a
  "Resend SMS" action.

### Testing

- xUnit tests against EF Core InMemory/SQLite for `AppointmentService`: overlap rejection,
  invalid range rejection, SMS sent on create, SMS failure recorded without failing create.
- `dotnet build` for both the Windows and Android targets.

## Out of scope (YAGNI)

Authentication/multi-user, recurring appointments, push notifications, calendar sync.
