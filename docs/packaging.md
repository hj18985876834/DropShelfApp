# EdgeTuck Packaging and Release

EdgeTuck ships as a traditional Inno Setup installer for current-user Windows installs.

This document is the source of truth for local packaging, installer behavior, GitHub Release publishing, and the manual update manifest.

## Prerequisites

Run packaging from Windows PowerShell, not from WSL shell commands.

Required tools:

* .NET SDK that can build `net10.0-windows`
* Inno Setup 6 at `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`
* Windows SDK `signtool.exe`
* An Authenticode code-signing certificate available to the release machine

The release packaging script reads signing configuration from environment variables:

```powershell
$env:EDGE_TUCK_SIGN_CERT_PATH = "C:\path\to\certificate.pfx"
$env:EDGE_TUCK_SIGN_CERT_PASSWORD = "<certificate password>"
$env:EDGE_TUCK_TIMESTAMP_URL = "http://timestamp.digicert.com"
```

`EDGE_TUCK_TIMESTAMP_URL` is optional and defaults to DigiCert's timestamp server.
Do not commit certificates, certificate passwords, or machine-specific signing paths.

The shared product icon lives at:

```text
src\DropShelf.App\Assets\DropShelf.ico
```

The app executable, tray icon, installer icon, Start Menu shortcut, desktop shortcut, and uninstall entry all use this icon.

The installer version, application assembly metadata, release tag, and update manifest version must stay aligned.

For the current baseline release:

```text
Version: 0.1.4
Release tag: v0.1.4
Main branch commit: v0.1.4 tag target
Installer size: 51119162 bytes
Installer SHA256: 9ab815743340b58e411e6a7c3f9319c95fa26e5a35733fc94956b39c4f1f601d
Release page: https://github.com/hj18985876834/DropShelfApp/releases/tag/v0.1.4
Installer URL: https://github.com/hj18985876834/DropShelfApp/releases/download/v0.1.4/EdgeTuckSetup.exe
```

## Quality Gate

Run these checks before creating the installer:

```powershell
dotnet build .\DropShelf.sln
dotnet test .\DropShelf.sln --no-build
dotnet format .\DropShelf.sln --verify-no-changes --verbosity minimal
```

Do not package if any command fails.

## Clean Generated Output

Before a formal package build, remove old local outputs so the final installer is known to come from the current source tree:

```bash
rm -rf artifacts installer/Output
```

`artifacts/` and `installer/Output/` are generated outputs and must not be committed.

## Publish

Create the Windows x64 self-contained app output:

```powershell
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
```

The installer script reads files from `artifacts\publish\win-x64`. The expected executable is:

```text
artifacts\publish\win-x64\DropShelf.App.exe
```

## Sign And Build Installer

For formal releases, prefer the packaging script so signing and verification cannot be skipped:

```powershell
.\scripts\package-release.ps1
```

The script:

1. Publishes the self-contained Windows x64 app.
2. Signs `artifacts\publish\win-x64\DropShelf.App.exe`.
3. Verifies the app signature with `signtool verify /pa /tw /v`.
4. Compiles the Inno Setup installer.
5. Signs `installer\Output\EdgeTuckSetup.exe`.
6. Verifies the installer signature.
7. Prints the installer byte size and SHA256 for `updates/latest.json`.

Manual compilation is still useful for local installer iteration.

Compile the Inno Setup script:

```powershell
& "D:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\DropShelf.iss
```

The installer output is:

```text
installer\Output\EdgeTuckSetup.exe
```

After compilation and signing, record the installer size and SHA256:

```bash
sha256sum installer/Output/EdgeTuckSetup.exe
stat -c '%s %n' installer/Output/EdgeTuckSetup.exe
```

The SHA256 value must be copied into `updates/latest.json` before publishing a release that clients can download.

## Install Behavior

The installer is intentionally per-user:

* It requests the lowest available privileges and should not show an administrator prompt during the normal flow.
* It installs to `%LOCALAPPDATA%\Programs\EdgeTuck`.
* It creates a Start Menu shortcut.
* It offers an optional desktop shortcut, unchecked by default.
* Its setup UI supports English and Simplified Chinese.
* It does not enable startup with Windows. Startup remains controlled by the app setting backed by the HKCU Run key.
* It keeps the install directory fixed and does not offer a custom directory page in V1.
* It removes legacy `DropShelf` Start Menu and desktop shortcuts during install so renamed upgrades do not leave stale entry points.

If EdgeTuck is already running, Setup or Uninstall detects the app mutex and asks the user to close the app before continuing.

## Uninstall Behavior

Uninstall removes the installed files and shortcuts created by the installer.
It also removes legacy `DropShelf` Start Menu and desktop shortcuts left by pre-rename builds.

Uninstall also removes the app's HKCU startup value:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\EdgeTuck
```

For upgrades from pre-rename builds, uninstall also removes the legacy `DropShelf` Run value if it exists.

User data is intentionally preserved:

```text
%LOCALAPPDATA%\DropShelf
```

Do not add uninstall rules that delete this directory unless the product requirements change.

## Update Contract

EdgeTuck uses a GitHub-based update flow. The app may automatically check for update metadata, but it does not silently download or install updates in the background.

The app fetches this manifest from the `main` branch:

```text
https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json
```

`updates/latest.json` must contain:

```json
{
  "version": "0.1.4",
  "installerUrl": "https://github.com/hj18985876834/DropShelfApp/releases/download/v0.1.4/EdgeTuckSetup.exe",
  "sha256": "9ab815743340b58e411e6a7c3f9319c95fa26e5a35733fc94956b39c4f1f601d",
  "sizeBytes": 51119162,
  "releaseDate": "2026-07-19",
  "mandatory": false,
  "branding": {
    "displayName": "EdgeTuck",
    "descriptions": {
      "zh-CN": "EdgeTuck 是一款贴边常驻的 Windows 临时收纳工具，适合在整理文件、收集素材、搬运文本链接或处理中途截图时，把内容先放到屏幕边缘，稍后再复制、打开、定位、移除或拖回其它窗口。所有数据保存在本机，文件和文件夹仅记录原始路径，粘贴图片保存到本地应用数据目录，不需要账号，也不会上传到云端。",
      "en-US": "EdgeTuck is a local Windows edge shelf for temporarily holding files, folders, text, links, and images while you organize work, collect references, move snippets, or handle screenshots. It keeps data on this PC, stores files and folders as original path references, saves pasted images under local app data, and does not require an account or cloud service."
    }
  },
  "releaseNotes": {
    "zh-CN": "新增卡片搜索，可和类型筛选一起使用，更快找到暂存内容。支持从剪贴板批量导入多条有效路径或链接，并按类型创建记录和显示加入结果。支持多选当前可见卡片，可批量移除或复制；同类型内容会按文件、文件夹、图片、文本和链接分别处理，混合类型复制会提示先选择同一类型。新增全局快捷键，可显示或隐藏 EdgeTuck，并在后台把当前剪贴板内容加入收纳栏。设置中的使用方法说明更清晰，检查更新时更新详情展示更稳定。",
    "en-US": "Added card search that works together with type filtering so shelf items are easier to find. Added batch import from clipboard text containing multiple valid paths or links, with typed item creation and type-specific add feedback. Added multi-select for visible cards with batch remove and copy; same-type content is handled separately for files, folders, images, text, and links, while mixed-type copy asks you to select one type first. Added global shortcuts to show or hide EdgeTuck and add current clipboard content in the background. Improved the settings usage guide and made update details display more consistently."
  }
}
```

Manifest rules:

* `version` is the semantic app version without the `v` prefix.
* `installerUrl` must use HTTPS and point to the GitHub Release asset, not a commit page, branch page, or raw repository file.
* `sha256` must match the uploaded `EdgeTuckSetup.exe` exactly.
* `sizeBytes` must match the uploaded asset exactly.
* `branding.displayName` and bilingual `branding.descriptions` drive the settings-page software name and introduction after a successful update check, even when no newer version is available.
* `releaseNotes` must include both `zh-CN` and `en-US`.
* If GitHub is unreachable during a manual check, the app should show a check-update failure message and make no local changes except non-user-visible automatic check throttling timestamps.

The app compares the local application version against the manifest version. If the manifest version is newer, it downloads the installer to:

```text
%LOCALAPPDATA%\DropShelf\updates\<version>\<installer file name from manifest URL>
```

The settings page shows the target version, release date, installer size, mandatory flag, SHA256 summary, and localized release notes before installation.
The user must confirm installation before EdgeTuck downloads the installer.
The downloaded file is launched only after SHA256 and byte-size verification succeeds.
Before launching the installer, the app stores the pending update version under `%LOCALAPPDATA%\DropShelf\settings.json`.
On the first successful launch of the updated version, EdgeTuck shows a tray notification once and clears the pending update marker.

Automatic update-check policy lives in `AppSettings.AutoUpdateCheckMode`:

* `Never`: no automatic manifest checks.
* `Daily`: at most once every 24 hours.
* `Weekly`: at most once every 7 days and the default for new installs.

Automatic checks only fetch the manifest. They never download the installer and never launch setup without the user's click.

## GitHub Release Procedure

Use `main` as the maintained source branch and GitHub repository default branch.
Use semantic version tags for releases.
GitHub Release creation can be done from WSL when `gh` is installed and
authenticated there; Windows PowerShell does not need `gh` as long as release
upload is performed from WSL.

For each release:

1. Update app and installer version metadata.
2. Run the quality gate.
3. Clean generated output.
4. Run `.\scripts\package-release.ps1` to publish, sign, build, sign the installer, verify signatures, and compute installer size/SHA256.
5. Confirm both the app executable and installer pass `signtool verify /pa /tw /v`.
6. Update `updates/latest.json` with the new version, asset URL, signed installer SHA256, size, release date, bilingual branding, and bilingual notes.
7. Commit and push all source, installer script, docs, and manifest changes to `main`.
8. Create a GitHub Release with tag `v<version>` targeting the latest `main` commit.
9. Upload the exact `EdgeTuckSetup.exe` that was hashed.
10. Verify the GitHub Release asset size and SHA256 match `updates/latest.json`.
11. Verify the raw manifest URL returns the committed manifest.
12. Ask the user to run the update flow and confirm the latest version installs.

If GitHub's default branch is not `main`, switch it before publishing:

```bash
gh repo view hj18985876834/DropShelfApp --json defaultBranchRef --jq '.defaultBranchRef.name'
gh repo edit hj18985876834/DropShelfApp --default-branch main
git remote set-head origin -a
git remote show origin
```

`git remote show origin` must report:

```text
HEAD branch: main
```

Example GitHub CLI command:

```bash
gh release create v0.1.1 installer/Output/EdgeTuckSetup.exe \
  --repo hj18985876834/DropShelfApp \
  --target 161b684b0345ce39f1201ef47c1560fc14fde61c \
  --title "EdgeTuck 0.1.1" \
  --notes-file release-notes.md
```

Verify the uploaded release metadata:

```bash
gh release view v0.1.1 \
  --repo hj18985876834/DropShelfApp \
  --json tagName,targetCommitish,name,url,assets
git ls-remote --heads origin main
git ls-remote --tags origin "v0.1.1" "v0.1.1^{}"
gh repo view hj18985876834/DropShelfApp --json defaultBranchRef --jq '.defaultBranchRef.name'
curl -L https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json
```

If a full download from GitHub is fast enough, verify the published asset hash:

```bash
curl -L -o /tmp/EdgeTuckSetup-v0.1.1.exe \
  https://github.com/hj18985876834/DropShelfApp/releases/download/v0.1.1/EdgeTuckSetup.exe
sha256sum /tmp/EdgeTuckSetup-v0.1.1.exe
stat -c '%s %n' /tmp/EdgeTuckSetup-v0.1.1.exe
```

If the GitHub asset download is too slow to complete, do not leave the release
unverified. Confirm the GitHub Release asset size through `gh release view`,
confirm the uploaded local installer SHA256 matches `updates/latest.json`, and
require a real user-side update/install success before treating the release as
complete.

Correct release URLs use this shape:

```text
https://github.com/hj18985876834/DropShelfApp/releases/tag/v0.1.1
https://github.com/hj18985876834/DropShelfApp/releases/download/v0.1.1/EdgeTuckSetup.exe
```

Do not use these as release asset URLs:

```text
https://github.com/hj18985876834/DropShelfApp/commits/v0.1.0
https://github.com/hj18985876834/DropShelfApp/releases/tag/main
https://github.com/hj18985876834/DropShelfApp/releases/download/main/EdgeTuckSetup.exe
```

`main` is a branch name. Do not create or keep a `main` tag for releases.

## Release Notes

EdgeTuck is a bilingual app in V1. Keep English and Chinese runtime resources when optimizing the publish output.

The Release publish is configured to omit app debug symbols from the installer payload. Keep local build artifacts and installer output out of source control.

## Supported and Unsupported Scenarios

Supported:

* Windows x64 current-user install.
* Installing on another Windows x64 computer without preinstalling the .NET runtime.
* English and Simplified Chinese setup UI.
* Bilingual app UI.
* Manual update check and installer download through GitHub Release assets.
* Fixed install directory at `%LOCALAPPDATA%\Programs\EdgeTuck`.

Not supported in V1:

* Custom install directory selection.
* Machine-wide install under `Program Files`.
* Silent background updates.
* Automatic update installation without user action.
* Deleting user data during uninstall.

## Manual Smoke Test

After building `EdgeTuckSetup.exe`, validate on Windows:

1. Run `installer\Output\EdgeTuckSetup.exe` as a normal user.
2. Confirm there is no administrator prompt.
3. Confirm EdgeTuck is installed under `%LOCALAPPDATA%\Programs\EdgeTuck`.
4. Launch EdgeTuck from the Start Menu shortcut.
5. Reinstall while EdgeTuck is running and confirm the installer asks for the app to be closed.
6. Install again with the optional desktop shortcut selected and launch from that shortcut.
7. Uninstall from Windows Apps settings or the uninstaller entry.
8. Confirm `%LOCALAPPDATA%\Programs\EdgeTuck` is removed.
9. Confirm `%LOCALAPPDATA%\DropShelf` remains.
10. Reinstall and confirm EdgeTuck launches successfully.

## Release Verification Checklist

Before telling users to install a release, verify:

1. GitHub reports `main` as the repository default branch.
2. `git remote show origin` reports `HEAD branch: main`.
3. `git ls-remote --heads origin main` points to the intended release commit.
4. `git ls-remote --tags origin "v<version>" "v<version>^{}"` resolves to the
   same intended release commit.
5. The GitHub Release page is `/releases/tag/v<version>`.
6. The Release asset is named `EdgeTuckSetup.exe`.
7. The Release asset size equals `updates/latest.json` `sizeBytes`.
8. The Release asset SHA256 equals `updates/latest.json` `sha256`.
9. The manifest `installerUrl` uses `/releases/download/v<version>/EdgeTuckSetup.exe`.
10. The manifest includes `branding.displayName` and bilingual `branding.descriptions`.
11. App check-update reports "latest" when local version equals manifest version and refreshes the settings-page branding.
12. A user has successfully obtained and installed the latest version through
    the published update flow.
