# Scheduler

A cross-platform appointment scheduler for **Android and Windows**, built with **.NET 10 MAUI**,
backed by **SQL Server**, that texts the client an SMS confirmation the moment an appointment is booked.

```
Scheduler.sln
├── src/Scheduler.Shared      DTOs and enums shared by the API and the app
├── src/Scheduler.Api         ASP.NET Core minimal API · EF Core (SQL Server) · Twilio SMS
├── src/Scheduler.App         .NET MAUI app (net10.0-android, net10.0-windows) · MVVM
└── tests/Scheduler.Api.Tests xUnit tests for scheduling rules and SMS behaviour
```

The app never talks to SQL Server or Twilio directly. It calls the API over HTTPS/JSON, so the
database connection string and SMS credentials stay on the server and the same backend serves
phones and desktops.

## Features

- Week strip + daily agenda with colour-tagged appointment cards, pull to refresh
- Clients directory with search, avatars, one-tap "Book"
- Appointment editor: client, title, date, start/end, location, notes, colour tag, status
- **SMS on booking** (confirmation), on reschedule (new time), and on cancellation
- SMS delivery status shown on each appointment with a *Resend* action
- Overlap protection: a client can't be double-booked
- Modern indigo → violet → pink gradient UI, rounded cards, **System / Light / Dark** theme toggle
- Settings page to point the app at any API host (emulator, LAN, cloud)

## Prerequisites

- .NET 10 SDK with the MAUI workload: `dotnet workload install maui`
- SQL Server (LocalDB, Express, full, or Azure SQL). The default connection string uses
  `(localdb)\SchedulerLocal`; create it with `SqlLocalDB create SchedulerLocal -s` or edit
  `src/Scheduler.Api/appsettings.json`.
- For Android: a **JDK 21** (JDK 25 is rejected by the Android toolchain) and the Android SDK.
  Without admin rights, unzip [Microsoft OpenJDK 21](https://aka.ms/download-jdk/microsoft-jdk-21-windows-x64.zip)
  into `C:\Android\` and let .NET download the SDK:

  ```powershell
  dotnet build src/Scheduler.App -f net10.0-android -t:InstallAndroidDependencies `
      -p:AndroidSdkDirectory=C:\Android\android-sdk `
      "-p:JavaSdkDirectory=C:\Android\jdk-21.0.12.1+1" `
      -p:AcceptAndroidSDKLicenses=True
  ```

  Then build/run with the same two `-p:` properties (or set `ANDROID_HOME` and `JAVA_HOME`).
- A Twilio account for real SMS (optional, see below)

## Run the API

```powershell
cd src/Scheduler.Api
dotnet run --launch-profile http        # listens on http://0.0.0.0:5180
```

On first start in Development the EF Core migration creates the `SchedulerDb` database and
tables (`Clients`, `Appointments`, `SmsMessages`).

Open `http://localhost:5180/api/health` to confirm it is up and see which SMS provider is active.
OpenAPI document: `http://localhost:5180/openapi/v1.json`.

### Configure SMS (Twilio)

Without credentials the API uses `LoggingSmsSender`, which writes the message text to the console
and records it as *Sent* with a `fake:` provider id, so the entire flow works offline.

To send real texts, set the three Twilio values with user secrets (or environment variables):

```powershell
cd src/Scheduler.Api
dotnet user-secrets set "Twilio:AccountSid" "ACxxxxxxxx"
dotnet user-secrets set "Twilio:AuthToken"  "your_auth_token"
dotnet user-secrets set "Twilio:FromNumber" "+15550001234"   # or a Messaging Service SID (MG...)
```

`Scheduler:BusinessName` and `Scheduler:TimeZone` in `appsettings.json` control the wording and
the time zone used inside the SMS.

## Run the app

**Windows**

```powershell
dotnet build src/Scheduler.App -f net10.0-windows10.0.19041.0 -t:Run
```

**Android (emulator or USB device)**

```powershell
dotnet build src/Scheduler.App -f net10.0-android -t:Run `
    -p:AndroidSdkDirectory=C:\Android\android-sdk "-p:JavaSdkDirectory=C:\Android\jdk-21.0.12.1+1"
```

Or open `Scheduler.sln` in Visual Studio 2022 17.14+ and pick an Android emulator as the target.

The app defaults to `http://localhost:5180` on Windows and `http://10.0.2.2:5180` on the Android
emulator (the emulator's alias for the host PC). On a physical phone open **Settings → API URL**
and enter the PC's LAN address, e.g. `http://192.168.1.20:5180`. The API binds to `0.0.0.0`, so
only the Windows firewall needs to allow port 5180.

## Tests

```powershell
dotnet test
```

Covers: SMS sent on create, SMS failure never blocks the booking, overlap rejection, invalid
range rejection, reschedule/cancel notifications, resend, phone normalisation.

## API

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/health` | Liveness + active SMS provider |
| GET/POST | `/api/clients` | List (`?search=`) / create |
| GET/PUT/DELETE | `/api/clients/{id}` | Read / update / delete |
| GET/POST | `/api/appointments` | List (`?from=&to=` UTC) / create (**sends SMS**) |
| GET/PUT/DELETE | `/api/appointments/{id}` | Read / update (SMS on time change or cancel) / delete |
| POST | `/api/appointments/{id}/resend-sms` | Re-send the confirmation |

Validation failures return RFC 7807 problem details with a 400 status.

## Design notes

See `docs/superpowers/specs/2026-10-08-maui-scheduler-design.md` for the approaches considered
and the reasoning behind the API-in-the-middle architecture.
