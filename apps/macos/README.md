# macOS App Skeleton

Planned stack: Swift + AppKit/SwiftUI hybrid menu bar app.

## Current MVP UI (implemented as SwiftUI skeleton)

- menu bar status panel (MenuBarExtra)
- trusted devices window with revoke-ready list structure
- pairing requests window with approve/reject actions
- sync history window
- manual sync action from menu panel
- status panel includes trusted-device and pending-pairing counters
- visual polish: gradient status panel and richer metadata labels

## MVP integration points

- implement clipboard listener + menu bar status
- implement SecureStore via Keychain
- connect SyncTransport (MQTT over WSS)
- trusted devices and history window

## Build note

- runtime build/package requires macOS + Xcode.
