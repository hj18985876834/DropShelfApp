# Feature Spec: Installer

## Goal

Package EdgeTuck as a traditional Windows installer for current-user installation.

## Branch

Initial implementation used `feature/installer`. Ongoing release maintenance uses `main`.

## Dependencies

* App can build and publish.
* Basic app launch is stable.

## User Flow

1. User downloads `EdgeTuckSetup.exe`.
2. User runs installer.
3. Installer does not require administrator privileges.
4. App installs for current user.
5. Start Menu shortcut is created.
6. Optional desktop shortcut can be selected.
7. User launches app.
8. User can uninstall from Windows.

## Detailed Behavior

The full packaging and release procedure is maintained in:

```text
docs/packaging.md
```

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
installer/Output/EdgeTuckSetup.exe
```

### Install Scope

* Current user only.
* No admin prompt for normal install.
* Install directory:

```text
%LOCALAPPDATA%/Programs/DropShelf/
```
* Custom install directory selection is not supported in V1.

### Shortcuts

* Start Menu shortcut required.
* Desktop shortcut optional and unchecked by default.

### Languages

* App UI supports Chinese and English.
* Installer UI supports English and Simplified Chinese.
* Release publish must preserve Chinese runtime resources.

### Uninstall

* Removes installed app files.
* Removes the EdgeTuck HKCU startup Run value and the legacy DropShelf Run value.
* Leaves `%LOCALAPPDATA%/DropShelf/` user data by default.
* Does not remove unrelated files.

### Startup

Installer does not enable start with Windows. Startup remains app setting controlled by HKCU Run key.

## UI States

Installer UI is standard Inno Setup UI. Do not customize heavily in V1.

## Data Contract

No app data schema changes.

Installer variables should align with:

* app name: `EdgeTuck`
* version: `0.1.0` until release process changes it
* executable: `DropShelf.App.exe`
* release tag: `v<version>`, for example `v0.1.0`

### Manual Update Manifest

DropShelf uses a manual GitHub-based update flow. The app checks:

```text
https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json
```

The manifest must point to a GitHub Release asset:

```text
https://github.com/hj18985876834/DropShelfApp/releases/download/v<version>/EdgeTuckSetup.exe
```

The manifest must include the semantic version, installer URL, SHA256, byte size, release date, mandatory flag, bilingual branding metadata, and bilingual release notes.

For `0.1.0`, the baseline release metadata is:

```text
Release tag: v0.1.0
Installer: EdgeTuckSetup.exe
Size: 51089185 bytes
SHA256: 5bf37f47db6eeedb434a3bc6d0dc4b080e9a1c37f56a54bdc80b6272e1f25055
Display name: EdgeTuck
```

The app downloads newer installers to:

```text
%LOCALAPPDATA%/DropShelf/updates/<version>/<installer file name from manifest URL>
```

The downloaded installer must pass SHA256 verification before launch.

## Edge Cases

* Existing install present.
* App currently running during install/uninstall.
* User data exists from previous version.
* Install path contains spaces.
* Unsigned installer warning.
* Publish directory missing.
* GitHub or raw manifest URL unreachable.
* Release asset URL does not match the manifest.
* Downloaded installer hash mismatch.
* A release tag named `main` exists and conflicts with the `main` branch.

## Acceptance Criteria

* Release publish succeeds.
* Inno Setup build succeeds.
* Installer runs without admin prompt.
* App installs for current user.
* Start Menu shortcut launches app.
* Optional desktop shortcut works when selected.
* Uninstaller is registered.
* Uninstall removes app files.
* Uninstall removes the EdgeTuck HKCU startup Run value and the legacy DropShelf Run value.
* User data under `%LOCALAPPDATA%/DropShelf/` remains.
* Reinstall can launch app.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass before packaging.
* GitHub Release tag uses `v<version>`, not `main`.
* Release asset size and SHA256 match `updates/latest.json`.
* Manual update check reports latest when local version equals manifest version.
* Manual update check synchronizes settings-page display name and introduction from `updates/latest.json` branding metadata.

## Tests

### Automated Checks

* Validate publish command exits successfully.
* Validate Inno compiler exits successfully.
* Validate installer SHA256 and size after build.

### Manual Windows Tests

* Install as normal user.
* Launch from Start Menu.
* Optional desktop shortcut.
* Uninstall from Windows Apps settings or uninstaller.
* Reinstall after uninstall.
* Verify no admin prompt in normal flow.
* Verify manual update check behavior.

## Files Likely Touched

* `installer/DropShelf.iss`
* `docs/packaging.md`
* `updates/latest.json`
* `src/DropShelf.App/DropShelf.App.csproj`
* release scripts if added later

## Out Of Scope

* Code signing.
* Silent background auto-update.
* MSIX.
* Microsoft Store.
* Per-machine install.
* Custom install directory selection.
* Removing user data during uninstall.
