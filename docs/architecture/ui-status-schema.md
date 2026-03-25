# Cross-Platform UI Status Schema

This file defines the normalized status fields now used by Windows, Android, iOS, and macOS UI layers.

## Connection state enum

Allowed values:

- `CONNECTED`
- `DEGRADED`
- `DISCONNECTED`

## Status fields

- `connectionState`: enum above
- `syncedOutCount`: total outgoing sync events
- `syncedInCount`: total incoming sync events
- `rejectedEventCount`: rejected/replayed/revoked-related events shown to user
- `trustedDeviceCount`: current trusted device total
- `pendingPairingCount`: number of invite-based pairing requests waiting for approval
- `lastErrorMessage`: last visible error summary, nullable

## Platform mappings

- Windows: `apps/windows/src/StatusViewModel.cs`
- Android: `apps/android/src/main/java/com/clipboardsync/StatusViewModel.kt`
- iOS: `apps/ios/Sources/StatusViewModel.swift`
- macOS: `apps/macos/Sources/StatusViewModel.swift`

## UI behavior baseline

- Manual sync increments `syncedOutCount` and `syncedInCount` in MVP simulation flow.
- Device revoke increments `rejectedEventCount` and writes `lastErrorMessage`.
- Status pages display enum value directly in current MVP UI skeleton.

## Settings-driven behavior (implemented in app-core)

- When `autoSyncEnabled=false`, non-manual send requests are rejected with `manual-mode-required`.
- Blacklisted app IDs are blocked by `sendTextFromApp(sourceAppId, text)` and recorded as history failures.
- `syncMode`, `themeMode`, `language`, and `webDevSyncEnabled` update runtime status snapshot.
- `pairingPolicy` supports two values:
	- `manual-approve`: invite join requests enter pending queue and require explicit approval.
	- `auto-approve-invite`: valid one-time invite requests are auto-approved and directly added to trusted devices.

## Pairing workflow status notes

- `requestPairingByInvite` creates a pending request in manual mode and increments `pendingPairingCount`.
- `approvePairingRequest` decrements `pendingPairingCount` and increments `trustedDeviceCount`.
- `rejectPairingRequest` decrements `pendingPairingCount` without changing `trustedDeviceCount`.

## Content support (current protocol + app-core)

- `text/plain`
- `text/html`
- `image/png`
- `application/octet-stream`
- `application/x-clipboard-file-ref`
