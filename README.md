# Telemetry Audit

**See exactly what Windows is phoning home — and turn it off with two clicks.** Telemetry Audit scans the registry, services, and scheduled tasks that Windows uses to collect your usage data, then lets you disable the ones you don't want.

No installer, no bloat, no telemetry of its own. One small WinForms exe, .NET Framework 4.8.

---

## Why

Windows has telemetry switches scattered across three layers:

- a registry policy (`AllowTelemetry`)
- two system services (`DiagTrack`, `dmwappushservice`)
- four scheduled tasks (Compatibility Appraiser, CEIP, USB CEIP, Program Data Updater)

Finding them by hand means editing the registry, opening `services.msc`, and digging through Task Scheduler. Telemetry Audit finds all of them in one scan and shows you what's on.

## Features

| | |
|---|---|
| **One-click scan** | Registry telemetry policy, advertising ID, telemetry services, and the known scheduled tasks — all in one list. |
| **Check and disable** | Check the boxes you want off, click "Disable selected" — the corresponding `reg add` / `sc config` / `schtasks` command runs for you. |
| **Non-destructive** | Shows you what it's about to change (the command line is built into each row); nothing is hidden. |
| **No install** | Single exe, ~20 KB. Run as admin, scan, disable, reboot. |

## Quick start

```bash
# build (uses the in-box C# compiler)
build.cmd

# run (admin required to change services / scheduled tasks)
bin\TelemetryAudit.exe
```

Two buttons:

1. **Scan** — lists every known telemetry knob, with red rows for anything currently on.
2. **Disable selected** — applies the registry / service / task changes for the checked rows. Reboot afterward.

## What it checks

**Registry**
- `HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowTelemetry`
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled`

**Services**
- `DiagTrack` (Connected User Experiences and Telemetry)
- `dmwappushservice` (Device Management WAP Push)

**Scheduled tasks**
- `\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser`
- `\Microsoft\Windows\Application Experience\ProgramDataUpdater`
- `\Microsoft\Windows\Customer Experience Improvement Program\Consolidator`
- `\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip`

## Tech

- C# / WinForms, .NET Framework 4.8
- `ServiceController` for services, `Registry` for registry, `schtasks /query` for scheduled tasks
- No third-party dependencies

## Positioning

Telemetry Audit is a **privacy tool for your own machine**: review and reduce what Windows collects from you. It does not block network traffic (pair with [NetAudit](https://github.com/walomosi/net-audit) for that), and it does not touch features you might want (Windows Update, Defender).

Not for: tampering with managed devices at work, or anything your sysadmin would not want changed.

## License

[MIT](LICENSE).

---

If this helps you quiet Windows down, a ⭐ is appreciated.
