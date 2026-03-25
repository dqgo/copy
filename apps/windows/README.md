# Windows App Skeleton

Planned stack: WinUI 3 + background tray process.

## Current MVP UI (implemented)

- status tab with connection and sync counters
- trusted devices tab with revoke action
- pairing requests tab with approve/reject actions
- manual sync button for foreground trigger
- visible desktop window to confirm app is running
- tray resident mode with quick actions (show/manual sync/exit)
- content quick send actions: text/html, image ref, file ref
- status panel now includes trusted-device and pending-pairing counters

## MVP integration points

- implement ClipboardReader/Writer
- SecureStore via DPAPI (implemented, persisted under LocalApplicationData/ClipboardSync)
- connect SyncTransport (MQTT over WSS)
- expose trusted device list and revocation UI

## Local run

```bash
dotnet run --project apps/windows/ClipboardSync.Windows.csproj
```
