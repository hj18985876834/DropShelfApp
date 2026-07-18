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
7. Legacy `DropShelf` shortcuts are cleaned up during renamed upgrades.
8. User launches app.
9. User can uninstall from Windows.

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
%LOCALAPPDATA%/Programs/EdgeTuck/
```
* Custom install directory selection is not supported in V1.

### Shortcuts

* Start Menu shortcut required.
* Desktop shortcut optional and unchecked by default.
* Legacy `DropShelf` Start Menu and desktop shortcuts are removed during install and uninstall.

### Languages

* App UI supports Chinese and English.
* Installer UI supports English and Simplified Chinese.
* Release publish must preserve Chinese runtime resources.

### Uninstall

* Removes installed app files.
* Removes the EdgeTuck HKCU startup Run value and the legacy DropShelf Run value.
* Removes legacy DropShelf Start Menu and desktop shortcuts.
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
* version: current semantic release version, for example `0.1.1`
* executable: `DropShelf.App.exe`
* release tag: `v<version>`, for example `v0.1.1`

### Manual Update Manifest

DropShelf uses a manual GitHub-based update flow. The app checks:

```text
https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json
```

The manifest must point to a GitHub Release asset:

```text
https://github.com/hj18985876834/DropShelfApp/releases/download/v<version>/EdgeTuckSetup.exe
```

The manifest must include the semantic version, HTTPS installer URL, SHA256, byte size, release date, mandatory flag, bilingual branding metadata, and bilingual release notes.

For `0.1.1`, the baseline release metadata is:

```text
Release tag: v0.1.1
Installer: EdgeTuckSetup.exe
Size: 51097188 bytes
SHA256: aa15de8d3232a8b2023fcedf1c7ae7f521365ac86a8451051beefd02ae43a9ca
Display name: EdgeTuck
Release page: https://github.com/hj18985876834/DropShelfApp/releases/tag/v0.1.1
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
* Downloaded installer hash or byte-size mismatch.
* A release tag named `main` exists and conflicts with the `main` branch.
* User cancels the install confirmation after reviewing release notes.

## Acceptance Criteria

* Release publish succeeds.
* Inno Setup build succeeds.
* Installer runs without admin prompt.
* App installs for current user.
* Start Menu shortcut launches app.
* Optional desktop shortcut works when selected.
* Legacy `DropShelf` shortcuts are removed during install/uninstall.
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
* Newer update checks show version, release date, installer size, SHA256 summary, mandatory flag, and release notes before install.
* User confirmation is required before update download/install.
* Downloaded installer hash and byte size must match the manifest before launch.
* Automatic update checks fetch metadata only and do not download or install.
* First launch after a successful update shows a one-time updated-version tray notice.
* User can obtain and install the latest version through the published update flow.

## Tests

### Automated Checks

* Validate publish command exits successfully.
* Validate Inno compiler exits successfully.
* Validate installer SHA256 and size after build.
* Validate app and installer Authenticode signatures after signing.

### Manual Windows Tests

* Install as normal user.
* Launch from Start Menu.
* Optional desktop shortcut.
* Uninstall from Windows Apps settings or uninstaller.
* Reinstall after uninstall.
* Verify no admin prompt in normal flow.
* Verify manual update check behavior.
* Verify automatic weekly metadata check behavior.
* Verify install confirmation and updated-version notice.
* Verify the latest published version can be downloaded and installed through the app update flow.

## Files Likely Touched

* `installer/DropShelf.iss`
* `scripts/package-release.ps1`
* `docs/packaging.md`
* `updates/latest.json`
* `src/DropShelf.App/DropShelf.App.csproj`
* release scripts if added later

## Out Of Scope

* Silent background auto-update.
* MSIX.
* Microsoft Store.
* Per-machine install.
* Custom install directory selection.
* Removing user data during uninstall.
