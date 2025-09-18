# Debug Script for IP Blocker Logger VPN Service

This script will help you debug the VPN service issues:

## Step 1: Clear logcat and install/run the app
```bash
adb logcat -c
adb install -r "bin\Debug\net9.0-android\com.example.ipblocker-Signed.apk"
adb shell am start -n com.example.ipblocker/.Platforms.Android.MainActivity
```

## Step 2: Monitor logcat for debugging
Run this in a separate terminal to see debug output:
```bash
adb logcat | grep -i -E "(FirewallVpnService|FirewallController|FATAL|AndroidRuntime|System.err)"
```

## Step 3: Test the VPN service manually
Once the app is running:
1. Add some test IP addresses to block (e.g., 8.8.8.8, 1.1.1.1)
2. Click "Start IP Blocking"
3. Watch the logcat output for debug messages

## Expected Debug Messages:
- `FirewallController: StartAsync called`
- `FirewallController: VPN permission already granted, starting service` (or permission request)
- `FirewallVpnService: OnCreate called`
- `FirewallVpnService: OnStartCommand called`
- `FirewallVpnService: Started foreground`
- `FirewallVpnService: VPN started successfully`
- `FirewallVpnService: Packet loop started`

## Common Issues to Look For:
1. **VPN Permission Issues**: Look for permission-related errors
2. **Foreground Service Issues**: Check for notification/foreground service errors
3. **VPN Interface Creation**: Look for "Failed to establish VPN interface"
4. **Service Crashes**: Look for exceptions in OnStartCommand or StartVpn

## Testing Network Blocking:
After the VPN service starts successfully:
1. Try accessing a website you've added to the block list
2. Check the logs for "FirewallVpnService: Loaded X blocked IPs"
3. Look for packet interception messages

## Build Commands:
```bash
dotnet clean
dotnet build -f net9.0-android -c Debug
```