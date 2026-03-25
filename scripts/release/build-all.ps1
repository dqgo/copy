param(
  [switch]$SkipBootstrap,
  [switch]$TriggerCloud
)

Push-Location "$PSScriptRoot\..\.."

if (-not $SkipBootstrap) {
  npm run bootstrap
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

npm run ci:validate
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path "artifacts" | Out-Null

# Windows package
if (Test-Path "apps/windows/*.csproj") {
  if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdks = dotnet --list-sdks
    if ([string]::IsNullOrWhiteSpace(($sdks | Out-String))) {
      Write-Host "[blocker] dotnet SDK not found; windows installer build skipped"
    } else {
      dotnet publish apps/windows -c Release -o artifacts/windows
    }
  } else {
    Write-Host "[blocker] dotnet SDK not found; windows installer build skipped"
  }
} else {
  Write-Host "[info] windows csproj not found, skipped packaging"
}

# Android package
if (Test-Path "apps/android/gradlew.bat") {
  Push-Location apps/android
  .\gradlew.bat assembleRelease
  New-Item -ItemType Directory -Force -Path "$PSScriptRoot\..\..\artifacts\android" | Out-Null
  Copy-Item -Path "app\build\outputs\apk\release\*.apk" -Destination "$PSScriptRoot\..\..\artifacts\android" -ErrorAction SilentlyContinue
  Pop-Location
} elseif ((Test-Path "apps/android/settings.gradle.kts") -or (Test-Path "apps/android/build.gradle.kts")) {
  $localGradle = "c:\tools\gradle\gradle-8.10.2\bin"
  if (Test-Path $localGradle) {
    $env:Path = "$localGradle;$env:Path"
  }
  if (Get-Command gradle -ErrorAction SilentlyContinue) {
    Push-Location apps/android
    $env:ANDROID_SDK_ROOT = "c:\Android"
    $env:ANDROID_HOME = "c:\Android"

    $signingDir = "$PSScriptRoot\..\..\artifacts\signing"
    New-Item -ItemType Directory -Force -Path $signingDir | Out-Null

    $storeFile = if ($env:ANDROID_RELEASE_STORE_FILE) { $env:ANDROID_RELEASE_STORE_FILE } else { "$signingDir\android-release.keystore" }
    $storePassword = if ($env:ANDROID_RELEASE_STORE_PASSWORD) { $env:ANDROID_RELEASE_STORE_PASSWORD } else { "androidrelease" }
    $keyAlias = if ($env:ANDROID_RELEASE_KEY_ALIAS) { $env:ANDROID_RELEASE_KEY_ALIAS } else { "clipboardsync" }
    $keyPassword = if ($env:ANDROID_RELEASE_KEY_PASSWORD) { $env:ANDROID_RELEASE_KEY_PASSWORD } else { "androidrelease" }

    if (-not (Test-Path $storeFile)) {
      if (Get-Command keytool -ErrorAction SilentlyContinue) {
        keytool -genkeypair -v -keystore $storeFile -storepass $storePassword -alias $keyAlias -keypass $keyPassword -keyalg RSA -keysize 2048 -validity 3650 -dname "CN=Clipboard Sync, OU=Dev, O=Clipboard Sync, L=N/A, S=N/A, C=US"
      } else {
        Write-Host "[blocker] keytool not found; android release signing skipped"
      }
    }

    gradle assembleRelease -PANDROID_RELEASE_STORE_FILE="$storeFile" -PANDROID_RELEASE_STORE_PASSWORD="$storePassword" -PANDROID_RELEASE_KEY_ALIAS="$keyAlias" -PANDROID_RELEASE_KEY_PASSWORD="$keyPassword"
    New-Item -ItemType Directory -Force -Path "$PSScriptRoot\..\..\artifacts\android" | Out-Null
    Copy-Item -Path "app\build\outputs\apk\release\*.apk" -Destination "$PSScriptRoot\..\..\artifacts\android" -ErrorAction SilentlyContinue
    Pop-Location
  } else {
    Write-Host "[blocker] gradle not found; android apk build skipped"
  }
} else {
  Write-Host "[info] android gradle wrapper not found, skipped packaging"
}

# iOS / macOS package (strict: no skip for full release)
if (-not $IsMacOS) {
  Write-Host "[blocker] full release requires macOS to build iOS/macOS packages"
  Pop-Location
  exit 2
}

if (-not (Get-Command xcodebuild -ErrorAction SilentlyContinue)) {
  Write-Host "[blocker] xcodebuild not found"
  Pop-Location
  exit 3
}

$iosProject = Get-ChildItem -Path "apps/ios" -Filter "*.xcodeproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $iosProject) {
  Write-Host "[blocker] iOS xcodeproj not found"
  Pop-Location
  exit 4
}

$macProject = Get-ChildItem -Path "apps/macos" -Filter "*.xcodeproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $macProject) {
  Write-Host "[blocker] macOS xcodeproj not found"
  Pop-Location
  exit 5
}

bash "scripts/release/apple-build.sh"
if ($LASTEXITCODE -ne 0) {
  Pop-Location
  exit $LASTEXITCODE
}

Write-Host "Release script finished"

if ($TriggerCloud) {
  if (Get-Command gh -ErrorAction SilentlyContinue) {
    Write-Host "[info] triggering cloud packaging workflow"
    gh workflow run release-packages.yml
    if ($LASTEXITCODE -ne 0) {
      Write-Host "[warn] failed to trigger cloud workflow via gh"
    }
  } else {
    Write-Host "[blocker] GitHub CLI not found; cloud trigger skipped"
  }
}

Pop-Location
