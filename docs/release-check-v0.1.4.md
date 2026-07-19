# EdgeTuck 0.1.4 Release Check

This is the internal release verification record. Public user-facing release
notes live in `release-notes-v0.1.4.md`.

## Scope

User-visible changes since `0.1.3`:

- Card search works together with type filters so shelf items are easier to find.
- External clipboard batch import can add multiple valid file paths, folder paths,
  image paths, and links as separate typed shelf items.
- Multi-select supports visible-item selection, range selection, batch remove,
  and same-type batch copy.
- Mixed-type copy is rejected with a clear prompt instead of copying partial or
  ambiguous content.
- Global shortcuts can show or hide EdgeTuck and add the current clipboard
  content in the background.
- Settings usage guidance is clearer, and update details display in a bounded
  details area without stretching the bottom action buttons.

## Release Metadata

- Version: `0.1.4`
- Release tag: `v0.1.4`
- Release title: `EdgeTuck 0.1.4`
- Release asset: `EdgeTuckSetup.exe`
- Installer URL: `https://github.com/hj18985876834/DropShelfApp/releases/download/v0.1.4/EdgeTuckSetup.exe`
- Release date: `2026-07-19`
- Mandatory update: `false`
- Signing: unsigned release

## Manifest Release Notes

Use these user-facing notes in `updates/latest.json` after the unsigned
installer size and SHA256 are known.

```json
{
  "zh-CN": "新增卡片搜索，可和类型筛选一起使用，更快找到暂存内容。支持从剪贴板批量导入多条有效路径或链接，并按类型创建记录和显示加入结果。支持多选当前可见卡片，可批量移除或复制；同类型内容会按文件、文件夹、图片、文本和链接分别处理，混合类型复制会提示先选择同一类型。新增全局快捷键，可显示或隐藏 EdgeTuck，并在后台把当前剪贴板内容加入收纳栏。设置中的使用方法说明更清晰，检查更新时更新详情展示更稳定。",
  "en-US": "Added card search that works together with type filtering so shelf items are easier to find. Added batch import from clipboard text containing multiple valid paths or links, with typed item creation and type-specific add feedback. Added multi-select for visible cards with batch remove and copy; same-type content is handled separately for files, folders, images, text, and links, while mixed-type copy asks you to select one type first. Added global shortcuts to show or hide EdgeTuck and add current clipboard content in the background. Improved the settings usage guide and made update details display more consistently."
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
- [x] Confirm `artifacts\publish\win-x64\DropShelf.App.exe` reports
  `FileVersion` `0.1.4.0` and `ProductVersion` `0.1.4`.
- [x] Compile `installer\Output\EdgeTuckSetup.exe` with Inno Setup.
- [x] Copy unsigned installer `SizeBytes` and SHA256 into `updates/latest.json`.
- [x] Commit and push the release source to `main`.
- [x] Create GitHub Release `v0.1.4` with `release-notes-v0.1.4.md`.
- [x] Verify `origin/main`, `v0.1.4`, and GitHub Release target point to the
  same commit.
- [x] Verify GitHub Release asset size and SHA256 match `updates/latest.json`.
- [x] Verify raw `updates/latest.json` on `main` returns version `0.1.4`.
- [ ] Verify app update flow from `0.1.3` to `0.1.4` completes successfully.

## Unsigned Packaging Validation

Completed on the current workstation for this unsigned release:

- Cleaned `artifacts` and `installer\Output`.
- Published self-contained `win-x64` output to `artifacts\publish\win-x64`.
- Confirmed `artifacts\publish\win-x64\DropShelf.App.exe` reports
  `FileVersion` `0.1.4.0` and `ProductVersion` `0.1.4`.
- Compiled the Inno Setup installer directly for packaging validation.

Unsigned validation installer:

- Path: `installer\Output\EdgeTuckSetup.exe`
- Size: `51119162` bytes
- SHA256: `9ab815743340b58e411e6a7c3f9319c95fa26e5a35733fc94956b39c4f1f601d`

These unsigned-installer values are copied into `updates/latest.json` for the
0.1.4 unsigned release.

## Published Release Validation

- Release page: `https://github.com/hj18985876834/DropShelfApp/releases/tag/v0.1.4`
- Release asset: `EdgeTuckSetup.exe`
- `origin/main`, `v0.1.4`, and the GitHub Release resolve to the same release
  source commit.
- GitHub asset size: `51119162` bytes
- GitHub asset digest:
  `sha256:9ab815743340b58e411e6a7c3f9319c95fa26e5a35733fc94956b39c4f1f601d`
- Raw `updates/latest.json` on `main` returns version `0.1.4` and the
  `v0.1.4` installer URL.

## Current Environment Notes

- Windows-side .NET SDK is available.
- Inno Setup is available at `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`.
- `origin` points to `git@github.com:hj18985876834/DropShelfApp.git`.
- `git remote show origin` reports `HEAD branch: main`.
- GitHub CLI is installed and authenticated with `repo` scope.
- This release continues the unsigned-release route confirmed for the previous
  release. Users may see Windows "unknown publisher" or SmartScreen warnings
  because the installer is not Authenticode-signed.
