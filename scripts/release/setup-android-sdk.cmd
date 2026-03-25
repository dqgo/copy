@echo off
setlocal
set ANDROID_SDK_ROOT=c:\Android
set PATH=c:\Android\cmdline-tools\latest\bin;c:\Android\platform-tools;c:\tools\gradle\gradle-8.10.2\bin;%PATH%

if not exist "%ANDROID_SDK_ROOT%\cmdline-tools\latest\bin\sdkmanager.bat" (
  echo [error] sdkmanager.bat not found
  exit /b 1
)

for /L %%i in (1,1,80) do echo y
for /L %%i in (1,1,80) do echo y

for /L %%i in (1,1,80) do echo y | "%ANDROID_SDK_ROOT%\cmdline-tools\latest\bin\sdkmanager.bat" --licenses
if errorlevel 1 (
  echo [error] accepting licenses failed
  exit /b 1
)

"%ANDROID_SDK_ROOT%\cmdline-tools\latest\bin\sdkmanager.bat" "platform-tools" "platforms;android-35" "build-tools;35.0.0"
if errorlevel 1 (
  echo [error] sdk component install failed
  exit /b 1
)

echo [ok] android sdk setup completed
exit /b 0
