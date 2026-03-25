#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT_DIR"

mkdir -p artifacts/ios artifacts/macos

if ! command -v xcodebuild >/dev/null 2>&1; then
  echo "[blocker] xcodebuild not found"
  exit 0
fi

build_ios() {
  local project_path=""
  if [ -d "apps/ios" ]; then
    project_path=$(find apps/ios -maxdepth 2 -name "*.xcodeproj" | head -n 1 || true)
  fi

  if [ -z "$project_path" ]; then
    echo "[info] iOS xcodeproj not found, skipped"
    return
  fi

  local scheme="${IOS_SCHEME:-ClipboardSynciOS}"
  local archive_path="$ROOT_DIR/artifacts/ios/ClipboardSynciOS.xcarchive"
  local export_path="$ROOT_DIR/artifacts/ios/export"
  local export_options="$ROOT_DIR/artifacts/ios/ExportOptions.plist"

  cat > "$export_options" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key>
  <string>app-store</string>
  <key>teamID</key>
  <string>${APPLE_TEAM_ID:-}</string>
  <key>signingStyle</key>
  <string>manual</string>
  <key>provisioningProfiles</key>
  <dict>
    <key>${IOS_BUNDLE_ID:-}</key>
    <string>${APPLE_PROFILE_NAME:-}</string>
  </dict>
</dict>
</plist>
EOF

  xcodebuild -project "$project_path" -scheme "$scheme" -configuration Release -archivePath "$archive_path" archive
  xcodebuild -exportArchive -archivePath "$archive_path" -exportPath "$export_path" -exportOptionsPlist "$export_options"
}

build_macos() {
  local project_path=""
  if [ -d "apps/macos" ]; then
    project_path=$(find apps/macos -maxdepth 2 -name "*.xcodeproj" | head -n 1 || true)
  fi

  if [ -z "$project_path" ]; then
    echo "[info] macOS xcodeproj not found, skipped"
    return
  fi

  local scheme="${MACOS_SCHEME:-ClipboardSyncMac}"
  local archive_path="$ROOT_DIR/artifacts/macos/ClipboardSyncMac.xcarchive"

  xcodebuild -project "$project_path" -scheme "$scheme" -configuration Release -archivePath "$archive_path" archive

  local app_path
  app_path=$(find "$archive_path/Products/Applications" -name "*.app" | head -n 1 || true)
  if [ -z "$app_path" ]; then
    echo "[warn] macOS app bundle not found after archive"
    return
  fi

  cp -R "$app_path" "$ROOT_DIR/artifacts/macos/"
  if command -v pkgbuild >/dev/null 2>&1; then
    pkgbuild --component "$app_path" --install-location /Applications "$ROOT_DIR/artifacts/macos/ClipboardSyncMac.pkg"
  fi
}

build_ios
build_macos

echo "apple-build completed"
