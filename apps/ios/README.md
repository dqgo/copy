# iOS App Skeleton

Planned stack: Swift + SwiftUI + native navigation.

## Current MVP UI (implemented as SwiftUI skeleton)

- app entry: ClipboardSyncIOSApp
- dashboard with Status/Devices/Pairing/History/Settings tabs
- status card includes trusted-device and pending-pairing counters
- foreground manual sync action
- device revoke flow via list delete
- pairing request approve/reject flow in dedicated tab
- visual polish: gradient backdrop and material cards

## MVP integration points

- implement foreground clipboard capture flow
- implement SecureStore via Keychain
- connect SyncTransport (MQTT over WSS)
- pairing + trusted devices + manual sync UI

## Build note

- runtime build/package requires macOS + Xcode.
