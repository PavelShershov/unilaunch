# UniLauncher v1.0 — Release Candidate source

This is the full source package prepared for Windows build and classroom validation.
It is **not a compiled Windows executable** and is not yet certified as working on the university image.

## Decisions
- C# / Windows Forms
- .NET 10 Windows target
- Windows 10 x64
- Dark UI
- Chrome + Yandex Browser
- `asInvoker`: no administrator elevation requested
- Cloud/network config beside the executable, with a LocalAppData recovery copy

## Included in this candidate
- Dark multi-tab interface
- Checkbox selection and last-selection persistence
- User-selected wallpaper path and wallpaper application
- User-level Left Alt + Shift registry preference
- Print Screen snipping registry preference and screen-clip launch attempt
- EXE / `.lnk` launcher list
- Chrome and Yandex executable detection, manual path selection, website list, selected URL launch
- Basic desktop icon position capture/restore prototype
- Cloud-first settings with timestamp-based newest-copy selection and LocalAppData fallback
- Named workspace profiles: save current module/app/site selection, apply, update, delete
- Diagnostics tab: OS/resolution, network/config write test, browser executable checks, local backup location
- Single-instance guard per Windows session
- Global exception logging to LocalAppData
- Logs and a test plan

## Known limitations — do not mistake these for completed guarantees
1. Desktop icon capture/restore uses Shell ListView item indexes, not stable filesystem identities. It may restore the wrong icon if the desktop icon order changes. Treat it as experimental until tested and improved.
2. Print Screen behavior varies by Windows 10 build and university policy. The code writes the per-user `PrintScreenKeyForSnippingEnabled` setting and invokes the screen-clip URI; it cannot guarantee the hotkey is unblocked.
3. Keyboard registry settings may require sign-out/sign-in and may be overridden by domain policy.
4. The app cannot guarantee that the university network drive is writable or mounted under the same path on every PC.
5. Cloud-first settings use a local recovery copy. This is not a multi-writer database: do not run two instances simultaneously against the same network folder.
6. Browser Manager launches the installed browser with its existing local profile. It does not migrate cookies or guarantee cross-PC sign-in persistence.
7. Profiles save module checkboxes plus enabled application paths and website URLs. They do not store credentials, cookies or browser tokens.
8. Network configuration uses a simple last-updated timestamp strategy. Simultaneous edits from different PCs can still conflict; use one UniLauncher instance at a time for a shared account.
9. Named profiles are now wired into the UI, but profile-file conflict resolution is still simple cloud-first/local fallback (no multi-writer merge).

## Build on your home Windows PC

For a one-click local build, double-click `BUILD.bat`. Alternatively, use the included GitHub Actions workflow `.github/workflows/windows-build.yml` for a Windows cloud build. Detailed instructions are in `BUILD_GUIDE_RU.md`.
1. Install the **.NET 10 SDK x64**. Visual Studio is not required for the one-click build.
2. Extract this archive to a local folder such as `C:\Projects\UniLauncher`.
3. Open PowerShell in the extracted folder.
4. Run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Build-Windows.ps1
```

Or run:

```powershell
dotnet publish .\UniLauncher\UniLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output folder:
`UniLauncher\bin\Release\net10.0-windows\win-x64\publish\`

Copy the **entire publish folder** to the university network drive. Do not copy only the EXE if other publish files are present.

## Safety and permissions
- The app does not request elevation and does not bypass university policies.
- It does not read, decrypt, export, or import browser cookies/passwords.
- Do not store passwords or session tokens in settings.
- Use only applications and sites you are authorized to access.
