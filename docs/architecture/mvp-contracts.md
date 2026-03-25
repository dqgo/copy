# MVP Service Contracts

This document defines the minimal platform-side service contracts required to integrate with the sync core.

## Core contracts

- ClipboardReader
- ClipboardWriter
- SecureStore
- SyncTransport
- DeviceRegistry

## ClipboardReader

Input: none
Output:
- text: string
- sourceAppId?: string
- capturedAt: ISO8601

## ClipboardWriter

Input:
- text: string
- reason: remote_sync | manual_paste
Output:
- success: boolean

## SecureStore

Keys:
- workspace_key
- device_private_key
- device_public_key
- key_version

Operations:
- get(key)
- set(key, value)
- delete(key)

## SyncTransport

Protocol: MQTT over TLS/WSS

Operations:
- connect()
- disconnect()
- subscribe(topic)
- publish(topic, envelope)

## DeviceRegistry

Operations:
- listTrusted()
- approvePairing(request)
- revoke(deviceId)

## Platform-specific notes

- iOS: no continuous background clipboard listening; use foreground-triggered sync.
- Android: support foreground service + fallback periodic sync.
- Windows/macOS: allow background listener with user-visible status.
