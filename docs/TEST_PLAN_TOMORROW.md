# UniLauncher candidate — classroom test plan

## Before class
- Build on home Windows 10/11 with .NET 10 SDK.
- Run the EXE once at home and confirm it opens.
- Copy the complete publish folder to the network share.
- Confirm the network share allows creating and editing files.
- Use non-sensitive public sites for first browser tests.

## Test
1. Start from the network share; confirm no UAC/admin prompt.
2. Change module checkboxes, save, close and reopen; confirm checkboxes persist.
3. Select a JPG/PNG and apply wallpaper.
4. Apply keyboard setting; test Alt+Shift, then sign out/in if needed.
5. Apply Screenshot Manager; test PrtSc and Win+Shift+S separately.
6. Add Notepad or another permitted EXE; launch it.
7. Add a permitted `.lnk`; launch it.
8. Add a public URL; test Chrome and Yandex separately.
9. Add several URLs; confirm tabs open in selected browsers.
10. Capture desktop layout, move one disposable icon, restore, and inspect carefully.
11. Disconnect network access only after backing up settings; verify fallback and error messages.
12. Create a profile named `Учёба`, enable a few modules and selected websites/apps, save it.
13. Change selections, apply `Учёба`, and confirm module/app/site selections restore.
14. Update and delete a test profile; close/reopen and confirm profile list persists.
15. Click Run diagnostics; confirm OS/resolution, cloud folder write test, browser paths and local backup are reported.
16. Launch an EXE twice; confirm second launch is skipped when the same executable is detectable as already running.
17. Review `UniLauncher.log` and `%LOCALAPPDATA%\\UniLauncher\\fatal.log` if a crash occurs.

## Report each issue
- Windows version/build (`winver`)
- Exact module
- Exact action
- Expected result
- Actual result
- Screenshot / log excerpt
