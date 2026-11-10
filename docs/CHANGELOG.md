# Changelog — UniLauncher Release Candidate

## Additional completion pass
- Added named workspace profiles (create, apply, update, delete).
- Profiles store module selection and enabled app/site entries.
- Added timestamp-based selection between cloud and local settings copies.
- Added local recovery storage under `%LOCALAPPDATA%\\UniLauncher`.
- Added single-instance guard to reduce accidental simultaneous writes.
- Added diagnostics for OS, display resolution, config-folder write access, browser executable paths, and local backup.
- Added global exception logging to local `fatal.log`.
- Added a best-effort duplicate-process check for direct EXE launches.
- Added resolution mismatch guard before desktop layout restore.
- Fixed browser preference initialization so opening the UI does not overwrite saved paths.

## Build preparation
- Added one-click `BUILD.bat` wrapper and a Windows build script that checks SDK, restores, publishes self-contained x64 single-file output, and creates a ZIP.
- Added a Russian build guide and explicit x64 solution configurations.
- Changed target framework to .NET 10 LTS because .NET 8 support ends on 2026-11-10.
- Logging now writes to both the network folder and LocalAppData when possible.
