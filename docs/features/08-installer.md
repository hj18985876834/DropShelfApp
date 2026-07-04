# Feature Spec: Installer

## Goal

Package DropShelf as a traditional Windows installer for current-user installation.

## Branch

`feature/installer`

## Dependencies

* App can build and publish.
* Basic app launch is stable.

## User Flow

1. User downloads `DropShelfSetup.exe`.
2. User runs installer.
3. Installer does not require administrator privileges.
4. App installs for current user.
5. Start Menu shortcut is created.
6. Optional desktop shortcut can be selected.
7. User launches app.
8. User can uninstall from Windows.

## Detailed Behavior

### Publish

Use Windows x64 self-contained publish output:

```powershell
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
```

### Inno Setup

Compiler path:

```powershell
& "D:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\DropShelf.iss
```

Installer output:

```text
installer/Output/DropShelfSetup.exe
```

### Install Scope

* Current user only.
* No admin prompt for normal install.
* Install directory:

```text
%LOCALAPPDATA%/Programs/DropShelf/
```

### Shortcuts

* Start Menu shortcut required.
* Desktop shortcut optional and unchecked by default.

### Uninstall

* Removes installed app files.
* Leaves `%LOCALAPPDATA%/DropShelf/` user data by default.
* Does not remove unrelated files.

### Startup

Installer does not enable start with Windows. Startup remains app setting controlled by HKCU Run key.

## UI States

Installer UI is standard Inno Setup UI. Do not customize heavily in V1.

## Data Contract

No app data schema changes.

Installer variables should align with:

* app name: `DropShelf`
* version: `0.1.0` until release process changes it
* executable: `DropShelf.App.exe`

## Edge Cases

* Existing install present.
* App currently running during install/uninstall.
* User data exists from previous version.
* Install path contains spaces.
* Unsigned installer warning.
* Publish directory missing.

## Acceptance Criteria

* Release publish succeeds.
* Inno Setup build succeeds.
* Installer runs without admin prompt.
* App installs for current user.
* Start Menu shortcut launches app.
* Optional desktop shortcut works when selected.
* Uninstaller is registered.
* Uninstall removes app files.
* User data under `%LOCALAPPDATA%/DropShelf/` remains.
* Reinstall can launch app.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass before packaging.

## Tests

### Automated Checks

* Validate publish command exits successfully.
* Validate Inno compiler exits successfully.

### Manual Windows Tests

* Install as normal user.
* Launch from Start Menu.
* Optional desktop shortcut.
* Uninstall from Windows Apps settings or uninstaller.
* Reinstall after uninstall.
* Verify no admin prompt in normal flow.

## Files Likely Touched

* `installer/DropShelf.iss`
* `docs/packaging.md`
* `src/DropShelf.App/DropShelf.App.csproj`
* release scripts if added later

## Out Of Scope

* Code signing.
* Auto-update.
* MSIX.
* Microsoft Store.
* Per-machine install.
* Removing user data during uninstall.
