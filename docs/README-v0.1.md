# AutoLogout v0.1 (Windows + Chrome / Edge)

> v0.2 배포 안내: README-v0.2.md 및 BUILD-STATUS.md를 먼저 읽으세요. 아래는 원본 v0.1 참고 문서입니다.

This is an **uncompiled prototype source project**. Build on Windows 10/11 x64 with .NET SDK 8+ and PowerShell. It has not been runtime-tested on Windows in the authoring environment.

## What it does

- Windows WPF desktop configuration window with HTTPS origin allowlist.
- Automatically starts at Windows **user sign-in** (after the user enables and saves the option).
- Attempts to signal cleanup at `WM_QUERYENDSESSION` (sign-out/restart/shutdown).
- Chrome and Microsoft Edge MV3 extensions perform `chrome.browsingData.remove()` for configured origins and show latest result in popup.
- A .NET Native Messaging host monitors settings and signals stored in `%LOCALAPPDATA%\AutoLogout` and forwards them to the extension.
- When a shutdown cleanup could not run because browser closed first, the **signal is stored** and is picked up the next time the browser launches with the extension enabled.

## Install / first run

1. Install .NET 8 SDK on Windows and ensure `dotnet` works in PowerShell.
2. Open PowerShell at repository folder. Run `powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1`.
3. Open Chrome `chrome://extensions` and Edge `edge://extensions`, turn on Developer mode, choose **Load unpacked**, and select the `extension` folder in each browser you want supported.
4. Copy the extension ID (32 characters a-p) from each browser. It is **not guaranteed to be the same ID** on both browsers.
5. Register the native host with e.g.:
   `powershell -ExecutionPolicy Bypass -File .\scripts\install-native-host.ps1 -HostExe "C:\path\to\AutoLogout\out\host\AutoLogout.Host.exe" -ChromeId "<chrome extension id>" -EdgeId "<edge extension id>"`
   Omit the argument for the browser not installed. Do **not** type the angle brackets around an actual ID.
6. Run `out\app\AutoLogout.App.exe` and enter exact HTTPS origins (e.g. `https://github.com`, **not** `github.com/path`). Click **설정 저장**.
7. Restart browser or reload the extension to establish its connection to the native host. Click **지금 정리** and check the extension popup (`ok: true` means the browser API resolved, **not** that the server session was revoked).
8. Check the Windows Run entry in HKCU to verify auto start. At future Windows sign-ins the application starts minimized; click its taskbar entry to open settings.

## Known limitations / cautions

- **No guarantee of logout before shutdown**: Windows can terminate the browser or host before cleanup; `WM_QUERYENDSESSION` may not arrive (crash, forced poweroff). Next browser launch normally picks up the pending signal.
- **Not server-side sign-out**: this only deletes browser-side site data. Server tokens/sessions may remain valid until expiry or explicit revocation; SSO can immediately sign in again.
- **Cookie domain scope**: deleting cookie data for `https://sub.example.com` may clear the registrable domain (`example.com`) and affect sibling sites. Other storage is removed only for specified origin(s). Multiple subdomains may need separate entries.
- **Profile coverage**: install and enable the extension separately for every Chrome/Edge browser profile; extension only acts inside the hosting profile.
- **The browser must run** for the extension to execute; the app does not attempt to corrupt locked browser databases or forcibly close tabs.
- Windows login startup does **not** imply cleanup before first user browser page opens. Native host/extension startup is asynchronous.
- The native host must remain in a stable disk location once registered. Do not move the `out/host` executable without rerunning the installer.
- A WPF application normally only receives the shutdown message while its window process is running; quitting the app disables shutdown signaling until next sign-in.
- Deleting LocalStorage/IndexedDB may affect offline app content and preferences for targeted origins. Test with an expendable account/profile first.
- For corporate PCs, extensions/native messaging may be restricted by organization policy.

## Project map

- `src/AutoLogout.App/`: Windows WPF editor and shutdown signal writer.
- `src/AutoLogout.Host/`: console native messaging bridge (standard output is framed JSON protocol).
- `extension/`: shared Chrome/Edge Manifest V3 browser extension.
- `scripts/build.ps1`: produces self-contained Windows x64 executables.
- `scripts/install-native-host.ps1`: registers both browsers' host key in HKCU.

## Troubleshooting

- Native host error in `chrome://extensions` > Service worker: verify manifest generated next to host exe, extension IDs, registry keys, and host exe absolute path.
- If Native Messaging host is disconnected or popup never updates, reload extension or restart browser.
- The app stores only website origins and event IDs in `%LOCALAPPDATA%\AutoLogout`; no passwords, cookies or tokens are copied into the app.

## Uninstall

- In WPF uncheck Windows automatic launch and save; remove extension in browsers.
- Delete HKCU keys `Software\Google\Chrome\NativeMessagingHosts\com.autologout.bridge` and `Software\Microsoft\Edge\NativeMessagingHosts\com.autologout.bridge`.
- Delete the build directory and `%LOCALAPPDATA%\AutoLogout` if you no longer need settings.

## Future work

- Signed MSIX installer, tray icon/background mode, event log with confirmed browser acknowledgements, browser-profile status detection, enterprise policy support, selective server-side logout adapters, automated integration tests.

