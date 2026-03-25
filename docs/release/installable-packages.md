# Installable Package Output Guide

## Current status in this workspace

- Windows: project skeleton created (apps/windows/ClipboardSync.Windows.csproj)
- Android: Gradle Android application skeleton created (apps/android)
- iOS: service code skeleton created, no xcodeproj yet
- macOS: service code skeleton created, no xcodeproj yet

## What can be packaged now

1. Windows (requires .NET SDK on build machine)
- Output target: artifacts/windows/*.exe
- Command: dotnet publish apps/windows -c Release -o artifacts/windows

2. Android (requires Gradle + Android SDK)
- Output target: apps/android/app/build/outputs/apk/release/*.apk
- Command: gradle assembleRelease (in apps/android)

## Apple platform requirement

- iOS/macOS installable package generation requires macOS + Xcode project + signing assets.
- Suggested outputs:
  - iOS: .ipa
  - macOS: .app / .pkg / .dmg

## One-command helper

- Script: scripts/release/build-all.ps1
- Root command: npm run release:all
- The script is strict for full release: missing macOS/Xcode/xcodeproj now fails the run instead of skipping Apple packaging.
