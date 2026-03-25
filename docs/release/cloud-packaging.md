# Cloud Packaging Pipeline

## Goal

Use GitHub Actions to produce installable artifacts for all available platforms.

## Workflow

- File: .github/workflows/release-packages.yml
- Trigger:
  - Manual (`workflow_dispatch`)
  - Git tags matching `v*`

## Jobs

1. validate
- Runs `npm run ci:validate`

2. package_windows
- Uses `actions/setup-dotnet`
- Runs `dotnet publish`
- Uploads `windows-package` artifact

3. package_android
- Uses `setup-java` + Gradle action
- Runs `gradle assembleRelease`
- Uploads `android-apk` artifact

4. package_apple
- Uses macOS runner + `scripts/release/apple-build.sh`
- Requires Apple signing secrets
- Uploads IPA/APP/PKG/DMG artifacts when present

## Required secrets for Apple signing

- APPLE_TEAM_ID
- APPLE_SIGN_IDENTITY
- APPLE_PROFILE_NAME
- IOS_BUNDLE_ID
- MACOS_BUNDLE_ID

## Local + cloud unified release

1. Local run
- `npm run release:all`

2. Optional cloud trigger from local machine (requires GitHub CLI auth)
- `npm run release:all:cloud`

The local release script validates and builds what is possible on the current machine, then can trigger cloud packaging workflow for full platform coverage.
