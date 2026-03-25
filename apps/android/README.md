# Android App Skeleton

Planned stack: Kotlin + Jetpack Compose + Material 3.

## Current MVP UI (implemented)

- compose material 3 dashboard with Status/Devices/Pairing/History/Settings tabs
- status metrics: connected/sent/received/rejected/trusted/pending-pairing/last error
- manual sync button for foreground-triggered path
- trusted device revoke action in list
- pairing request approve/reject actions in dedicated tab
- foreground notification quick actions: Manual Sync / Pause
- content quick send actions: text/html, image ref, file ref
- visual polish: gradient background, card hierarchy, trust chip badges

## MVP integration points

- implement ClipboardReader/Writer with version-aware restrictions
- implement SecureStore via Android Keystore
- connect SyncTransport (MQTT over WSS)
- foreground service mode and periodic fallback mode

## Local run

```bash
cd apps/android
gradle :app:assembleDebug
```

Note:

- build output directory is relocated to `C:/temp/ClipboardSyncAndroidBuild` to reduce OneDrive file lock issues on Windows.
