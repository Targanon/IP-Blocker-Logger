@echo off
echo ========================================
echo  IP Blocker Logger - Emulator Deploy
echo ========================================

echo.
echo Step 1: Building the app...
dotnet build -f net9.0-android -c Debug

if %ERRORLEVEL% NEQ 0 (
    echo Build failed! Please fix build errors first.
    pause
    exit /b 1
)

echo.
echo Step 2: Checking emulator...
adb devices

echo.
echo Step 3: Clearing logcat buffer...
adb logcat -c

echo.
echo Step 4: Stopping existing app instance...
adb shell am force-stop com.example.ipblocker

echo.
echo Step 5: Installing/updating app on emulator...
echo (This will update the existing installation)
adb install -r "bin\Debug\net9.0-android\com.example.ipblocker-Signed.apk"

if %ERRORLEVEL% NEQ 0 (
    echo Installation failed! Check if emulator is running.
    pause
    exit /b 1
)

echo.
echo Step 6: Starting the app...
adb shell am start -n com.example.ipblocker/crc640a325952741a90c5.MainActivity

echo.
echo Step 7: Starting logcat monitoring...
echo ========================================
echo LOGCAT OUTPUT (Press Ctrl+C to stop):
echo ========================================
echo Look for these key messages:
echo - FirewallController: StartAsync called
echo - FirewallVpnService: OnCreate called  
echo - FirewallVpnService: OnStartCommand called
echo - FirewallVpnService: Started foreground
echo - FirewallVpnService: VPN started successfully
echo - FirewallVpnService: Packet loop started
echo ========================================
echo.

adb logcat -s "System.Console:V" | findstr /i "FirewallVpnService FirewallController"