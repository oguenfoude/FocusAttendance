# Focus Attendance — AGENTS.md

## Build / Test / Run

| Command | Notes |
|---|---|
| `dotnet build FocusAttendance.sln -c Release` | Builds whole solution (7 projects) |
| `dotnet test tests/FocusAttendance.Tests/FocusAttendance.Tests.csproj -c Release` | Runs all 108 xUnit tests. Tests reference all source projects (Domain, Data, Hardware, Application) |
| `dotnet run --project src/FocusAttendance.App -c Release` | Launches WPF UI |
| `.\src\FocusAttendance.App\bin\Release\net10.0-windows10.0.19041.0\FocusAttendance.exe --selftest` | Headless self-test. Requires real ZKTeco terminal on port 4370; 0 devices is a valid result. Read `.\selftest.log` for details |

**Env vars:**
- `FOCUS_ADMIN_PASSWORD` — override default `Admin@2026` at first init
- Database path override via `AppHost` ctor; default `%LOCALAPPDATA%\FocusAttendance\focus-attendance.db`

## Architecture & Ownership

- **Domain** (`src/FocusAttendance.Domain`) — entities, enums, attendance rule engine. Pure, testable, no deps.
- **Data** (`src/FocusAttendance.Data`) — SQLite schema/migrations, repositories, seeding, backups, WAL, rolling backups (VACUUM INTO, 30 kept).
- **Hardware** (`src/FocusAttendance.Hardware`) — native ZKTeco protocol (TCP/UDP 4370), parsers, LAN scanner, optional COM SDK (`zkemkeeper`). Native is default; SDK used only if DLL installed & instantiable.
- **Application** (`src/FocusAttendance.Application`) — services, MVVM view models, exports. No WPF dependency.
- **App** (`src/FocusAttendance.App`) — WPF UI (Fluent 2), AR/FR resources, composition root.
- **Tests** (`tests/FocusAttendance.Tests`) — engine, protocol, DB, services, view models. 93 xUnit tests.

**Key repo-specific constraints:**
- **No fake data**: fresh DB has 0 employees, 0 devices, 0 punches. Only OPGI baseline seed (1 site, 12 depts/services, 2 shifts, SuperAdmin).
- **Zero hard delete**: every table exposes `IsArchived`, `ArchivedAtUtc`, `ArchiveReason`, `ArchivedBy` + restore path. Raw punches are immutable.
- **Strict dedup**: `UNIQUE(DeviceSerialNumber + DevicePin + PunchTimeUtc)`; re-pulling a device never duplicates a punch.
- **Rolling backups**: startup backup auto-created (`VACUUM INTO`), 30 kept. Manual backup/restore in Settings; a safety backup is taken before any restore.
- **Audit trail**: logins, device IP changes, archives/restores, HR adjustments, hardware operations written to `AuditLogs` / `HardwareOperationLogs`.
- **IP change pipeline** (SuperAdmin): verify serial at current IP → write IP/NetMask/GATE into NVRAM → persist new endpoint + audit → reboot → poll new IP every 2 s for 60 s → verify serial → sync clock → pull new punches → mark online.
- **DeviceMonitorService**: keeps enabled terminals status, latency, clock drift, user/punch counts live; reconnects after power/LAN loss; pulls new punches when device counter changes. Duplicate serial numbers and endpoints blocked at DB level.
- **Hardware ops require real terminal on LAN**. UI never fabricates devices, users or punches.

## UI

- Fluent 2 shell: `FluentWindow` + Mica, custom `TitleBar`, flat `NavigationView`, status bar.
- **No nested TabControl** and **no cascading popup windows**: all editing in a single in-page slide-over drawer; confirmations use one in-window Fluent `ContentDialog`; notifications use snackbars.
- Brand palette: OPGI emerald `#0F6B5C` + ochre `#B4761F` on warm sand; WPF-UI accent set to emerald (no generic blue).
- **Full RTL**: layout mirroring, flow-aware text alignment, LTR-isolated technical values, bilingual exports.
- Keyboard: `Ctrl+1…5` switch workspace, `Esc` closes a drawer.

## Tests

- `dotnet test` runs the full 108-test suite.
- Coverage highlights: Engine (4-scan hours, 2-scan lunch, grace boundaries, late/early leave, missing out-scan, single scan, off-day extra, free-time, anti-bounce, manual completion, mission/leave crediting, overtime multiplier, custom shifts), Protocol (checksum vector, TCP framing, time codec, COMM key, 8/16/40-byte parsing, 72/28-byte user round-trips, memory sizes), Database (WAL, unique serial/endpoint, punch dedup, soft archive/restore, backups, audit), Services (CRUD, device import, duplicate serial blocking, network-update validation, dynamic recalc after shift changes, CSV/HTML exports, dashboard KPIs), View models (directory loading, search, 7-day sheet, settings and devices loading).

## Dev tools

- `tools/UiRender` — deterministic screenshot harness that renders every workspace (light/dark, AR/FR) on a throw-away database; catches XAML runtime errors without driving the app.
- `docs/screenshots` — reference renders.

## Commands to avoid

- Do **not** run `dotnet test` without first building the solution (`dotnet build FocusAttendance.sln -c Release`), because the test project references all source projects and will fail if they aren't built.
- Do **not** expect the app to fabricate hardware: `--selftest` with 0 connected devices is a valid result; real ZKTeco terminal on LAN (port 4370) is required for full verification.