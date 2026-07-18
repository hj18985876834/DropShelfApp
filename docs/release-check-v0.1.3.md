# EdgeTuck 0.1.3 Release Check

This is the internal release verification record. Public user-facing release
notes live in `release-notes-v0.1.3.md`.

## Scope

User-visible changes since `0.1.2`:

- Shelf filtering and card management improvements.
- More stable card reorder interactions.
- File and folder card reliability improvements, including relinking missing paths.
- Clearer duplicate-add, file-action, and unavailable-state messages.
- Safer update flow with confirmation, download verification, and completion notice.
- Installer cleanup for legacy `DropShelf` shortcuts.

Internal/repository maintenance:

- Trellis and Codex project-local development files were moved out of Git tracking.

## Release Metadata

- Version: `0.1.3`
- Release tag: `v0.1.3`
- Release title: `EdgeTuck 0.1.3`
- Release asset: `EdgeTuckSetup.exe`
- Installer URL: `https://github.com/hj18985876834/DropShelfApp/releases/download/v0.1.3/EdgeTuckSetup.exe`
- Release date: `2026-07-18`
- Mandatory update: `false`
- Signing: unsigned release

## Manifest Release Notes

Use these user-facing notes in `updates/latest.json`.

```json
{
  "zh-CN": "新增收纳栏筛选，查找和整理卡片更方便。优化卡片拖拽排序，移动卡片时位置更清楚、操作更顺手。改进文件和文件夹卡片，支持在原路径失效后重新关联，并提供更清楚的重复添加、文件操作和异常状态提示。更新流程会先展示说明并由用户确认安装，安装包下载后会校验完整性，升级完成后会提示。升级安装时会清理旧版 DropShelf 快捷方式。",
  "en-US": "Added shelf filtering so cards are easier to find and organize. Improved card reordering so drag positions are clearer and the interaction feels smoother. Improved file and folder cards with relinking when the original path is no longer available, plus clearer duplicate-add, file-action, and unavailable-state messages. The update flow now shows release notes first and asks for confirmation before installation. Downloaded installers are verified before launch, EdgeTuck shows a notice after the update completes, and upgrade installs clean up legacy DropShelf shortcuts."
}
```

## Required Verification

- [x] `dotnet build .\DropShelf.sln`
- [x] `dotnet test .\DropShelf.sln --no-build`
- [x] `dotnet format .\DropShelf.sln --verify-no-changes --verbosity minimal`
- [x] `dotnet build .\DropShelf.sln -c Release`
- [x] `dotnet test .\DropShelf.sln -c Release`
- [x] Clean `artifacts` and `installer\Output`.
- [x] Publish self-contained `win-x64` output.
- [x] Compile `installer\Output\EdgeTuckSetup.exe` with Inno Setup.
- [x] Copy unsigned installer `SizeBytes` and SHA256 into `updates/latest.json`.
- [x] Confirm unsigned-release decision before publishing to users.
- [ ] Commit and push the release source to `main`.
- [ ] Create GitHub Release `v0.1.3` with `release-notes-v0.1.3.md`.
- [ ] Verify `origin/main`, `v0.1.3`, and GitHub Release target point to the same commit.
- [ ] Verify GitHub Release asset size and SHA256 match `updates/latest.json`.
- [ ] Verify raw `updates/latest.json` on `main` returns version `0.1.3`.
- [ ] Verify app update flow from `0.1.2` to `0.1.3` completes successfully.

## Unsigned Packaging Validation

Completed on the current workstation for this unsigned release:

- Cleaned `artifacts` and `installer\Output`.
- Published self-contained `win-x64` output to `artifacts\publish\win-x64`.
- Confirmed `artifacts\publish\win-x64\DropShelf.App.exe` reports
  `FileVersion` `0.1.3.0` and `ProductVersion` `0.1.3`.
- Compiled the Inno Setup installer directly for packaging validation.

Unsigned validation installer:

- Path: `installer\Output\EdgeTuckSetup.exe`
- Size: `51116729` bytes
- SHA256: `04b0b1a4131704f49b5000c738ac7b23bb28f600cc7244da3ce008c7cfd52e17`

These unsigned-installer values are copied into `updates/latest.json` for the
0.1.3 unsigned release. Users may see Windows "unknown publisher" or SmartScreen
warnings because the installer is not Authenticode-signed.

## Current Environment Notes

- Windows-side .NET SDK is available.
- Inno Setup is available at `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`.
- `signtool.exe` is available at
  `D:\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe`, but it is not in PATH
  from the current shell.
- `EDGE_TUCK_SIGN_CERT_PATH` is not set in the current shell.
- `.\scripts\package-release.ps1` currently stops with
  signing failure because the configured PFX file is empty.
- `EDGE_TUCK_SIGN_CERT_PATH` points to a `.pfx` file, but the file length is
  `0` bytes. `certutil -dump` reports `ERROR_INVALID_DATA`.
- `Get-FileHash` was not available in the Windows PowerShell session; WSL
  `sha256sum` was used only for the unsigned local validation package.
- `updates/latest.json` is switched to `0.1.3` using the unsigned installer size
  and SHA256 after the unsigned-release decision.
