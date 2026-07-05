# DropShelf Packaging

DropShelf ships as a traditional Inno Setup installer for current-user Windows installs.

## Prerequisites

Run packaging from Windows PowerShell, not from WSL shell commands.

Required tools:

* .NET SDK that can build `net10.0-windows`
* Inno Setup 6 at `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`

The shared product icon lives at:

```text
src\DropShelf.App\Assets\DropShelf.ico
```

The app executable, tray icon, installer icon, Start Menu shortcut, desktop shortcut, and uninstall entry all use this icon.

The installer version and application assembly metadata should stay aligned at `0.1.0` until the release process changes it.

## Quality Gate

Run these checks before creating the installer:

```powershell
dotnet build .\DropShelf.sln
dotnet test .\DropShelf.sln --no-build
dotnet format .\DropShelf.sln --verify-no-changes --verbosity minimal
```

Do not package if any command fails.

## Publish

Create the Windows x64 self-contained app output:

```powershell
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
```

The installer script reads files from `artifacts\publish\win-x64`. The expected executable is:

```text
artifacts\publish\win-x64\DropShelf.App.exe
```

## Build Installer

Compile the Inno Setup script:

```powershell
& "D:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\DropShelf.iss
```

The installer output is:

```text
installer\Output\DropShelfSetup.exe
```

## Install Behavior

The installer is intentionally per-user:

* It requests the lowest available privileges and should not show an administrator prompt during the normal flow.
* It installs to `%LOCALAPPDATA%\Programs\DropShelf`.
* It creates a Start Menu shortcut.
* It offers an optional desktop shortcut, unchecked by default.
* It does not enable startup with Windows. Startup remains controlled by the app setting backed by the HKCU Run key.
* It keeps the install directory fixed and does not offer a custom directory page in V1.

If DropShelf is already running, Setup or Uninstall detects the app mutex and asks the user to close the app before continuing.

## Uninstall Behavior

Uninstall removes the installed files and shortcuts created by the installer.

Uninstall also removes the app's HKCU startup value:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\DropShelf
```

User data is intentionally preserved:

```text
%LOCALAPPDATA%\DropShelf
```

Do not add uninstall rules that delete this directory unless the product requirements change.

## Release Notes

DropShelf is a bilingual app in V1. Keep English and Chinese runtime resources when optimizing the publish output.

The Release publish is configured to omit app debug symbols from the installer payload. Keep local build artifacts and installer output out of source control.

## Manual Smoke Test

After building `DropShelfSetup.exe`, validate on Windows:

1. Run `installer\Output\DropShelfSetup.exe` as a normal user.
2. Confirm there is no administrator prompt.
3. Confirm DropShelf is installed under `%LOCALAPPDATA%\Programs\DropShelf`.
4. Launch DropShelf from the Start Menu shortcut.
5. Reinstall while DropShelf is running and confirm the installer asks for the app to be closed.
6. Install again with the optional desktop shortcut selected and launch from that shortcut.
7. Uninstall from Windows Apps settings or the uninstaller entry.
8. Confirm `%LOCALAPPDATA%\Programs\DropShelf` is removed.
9. Confirm `%LOCALAPPDATA%\DropShelf` remains.
10. Reinstall and confirm DropShelf launches successfully.
