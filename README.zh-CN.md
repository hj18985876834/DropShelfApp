# EdgeTuck

[English](README.md) | [简体中文](README.zh-CN.md)

EdgeTuck 是一款轻量的 Windows 屏幕边缘暂存架工具，用来在日常工作中临时收纳文件、文件夹、文本、链接和图片。它运行在本地，文件和文件夹只保存原始路径引用，粘贴进来的图片会保存到本地应用数据目录，不需要账号，也不依赖云服务。

## 功能特性

- 屏幕边缘暂存架：通过一个小型边缘手柄显示或隐藏收纳栏。
- 固定收纳栏：可让悬停打开的收纳栏保持展开，直到手动隐藏。
- 拖入暂存：支持将文件、文件夹、文本、URL 和图片拖放到手柄或收纳栏。
- 拖出复用：支持把暂存卡片拖回资源管理器、桌面、编辑器、聊天窗口或其它支持拖放的应用。
- 卡片管理：按类型筛选，并可拖动类型图标区域调整卡片顺序。
- 快捷操作：复制、打开、在资源管理器中定位、移除单项、清空全部记录。
- 本地持久化：重启后恢复暂存记录和应用设置。
- 设置项：收纳栏位置、主题、密度、语言、开机自启动、自动检查更新策略。
- 检查更新：默认每周检查更新清单、安装前展示更新说明、下载安装包、校验 SHA-256、启动安装器，并在更新后提示版本。
- 托盘图标：显示收纳栏、隐藏收纳栏、打开设置、退出。

## 系统要求

- Windows 10 或更高版本，x64。
- 正常使用和当前用户安装都不需要管理员权限。
- 安装器会安装到当前用户目录下。

## 安装

从项目 Release 页面下载 `EdgeTuckSetup.exe` 并运行。

安装器会：

- 将 EdgeTuck 安装到 `%LOCALAPPDATA%\Programs\EdgeTuck`；
- 创建开始菜单快捷方式；
- 可选创建桌面快捷方式；
- 可在安装完成后启动 EdgeTuck；
- 创建标准 Windows 卸载入口；
- 在旧名称升级场景中清理遗留的 `DropShelf` 快捷方式。

如果只是开发验证，不安装也可以运行发布目录，但要运行完整发布目录，不要只复制单个 exe。当前验证输出路径是：

```text
artifacts/publish/win-x64/DropShelf.App.exe
```

如果从 WSL 发布验证版本，请先把完整的 `win-x64` 发布目录复制到 Windows 本地目录，再启动程序。

## 基本使用

1. 启动 EdgeTuck。
2. 使用屏幕边缘手柄打开收纳栏。
   如果希望鼠标移开后仍保持展开，可点击收纳栏顶部的固定按钮。
3. 将文件或文件夹拖到手柄或收纳栏，创建文件/文件夹暂存记录。
4. 在其它应用中复制文本、URL 或截图后，粘贴到 EdgeTuck。
5. 使用卡片上的操作按钮：
   - `复制`：复制路径、文本、URL 或图片。
   - `打开`：使用 Windows 默认方式打开文件、文件夹、URL 或图片。
   - `在资源管理器中定位`：在资源管理器中显示文件、文件夹或图片所在位置。
   - `移除`：移除该暂存记录。
6. 使用筛选控件查看全部或某一类型。
7. 拖动卡片的类型图标区域，在收纳栏内调整卡片顺序；被拖动卡片会呈现悬浮效果，拖动过程中卡片会动态移动。
8. 需要复用时，从卡片正文区域将卡片拖出到其它应用。

移除文件或文件夹记录不会删除原始文件或文件夹。清空暂存架也只会移除记录。对于粘贴进 EdgeTuck 的图片，移除图片记录时可能会删除应用自己保存的图片文件。

## 快捷键和托盘

- `Esc`：收纳栏获得焦点时隐藏收纳栏。
- `Delete`：移除当前选中项。
- `Enter`：打开当前选中项。
- `Ctrl+C`：复制当前选中项。
- 双击托盘图标：显示收纳栏。
- 托盘菜单：显示收纳栏、隐藏收纳栏、设置、退出。

## 设置

可以从收纳栏或托盘菜单打开设置。

可用设置：

- 收纳栏位置：选择手柄/收纳栏所在的屏幕边缘。
- 重置到右侧边缘：恢复默认收纳栏位置。
- 主题：跟随系统、浅色、深色。
- 密度：紧凑、舒适。
- 语言：中文、英文。
- 开机自启动：当前用户登录 Windows 后自动启动 EdgeTuck。
- 自动检查更新：从不、每天、每周。新安装默认每周检查。自动检查只获取更新清单，不会在未点击确认时下载安装包或启动安装器。

点击“应用”保存更改。设置文件保存在：

```text
%LOCALAPPDATA%\DropShelf\settings.json
```

## 开机自启动

EdgeTuck 使用当前用户的 Windows Run 注册表项：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
EdgeTuck = "<DropShelf.App.exe 路径>"
```

它不会写入系统级 `HKLM`，也不需要管理员权限。可以用下面命令验证：

```powershell
reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v EdgeTuck
```

如果需要手动移除：

```powershell
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v EdgeTuck /f
```

如果你从临时验证目录开启开机自启动，注册表会指向这个临时 exe 路径。验证完成后，如果不想保留这个路径，请在设置中关闭开机自启动，或手动删除注册表值。

## 更新

打开设置并点击“检查更新”，也可以保留自动检查。新安装默认最多每周检查一次更新清单。

应用会读取这个更新清单：

```text
https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json
```

当发现新版本时，EdgeTuck 会先显示目标版本、发布日期、安装包大小、SHA-256 摘要、是否必需更新和更新说明。用户确认后才会下载 `EdgeTuckSetup.exe`。下载完成后会根据清单校验 SHA-256 和文件大小，校验通过后才启动安装器，并退出当前运行的应用以便安装器替换文件。

新版本首次成功启动后，EdgeTuck 会通过托盘提示一次已更新到的版本。检查更新成功后，也会根据清单中的品牌信息刷新设置页的软件名称和介绍。应用不需要账号、遥测或云服务。

## 卸载

通过 Windows 设置卸载：

```text
设置 > 应用 > 已安装的应用 > EdgeTuck > 卸载
```

卸载器会移除已安装的程序文件和快捷方式。会移除 `EdgeTuck` 开机自启动 Run 值。对于旧版本升级场景，如果存在旧的 `DropShelf` Run 值，以及遗留的 `DropShelf` 开始菜单或桌面快捷方式，也会一并移除。

用户数据可能仍保留在：

```text
%LOCALAPPDATA%\DropShelf
```

如果希望彻底删除暂存记录、设置、下载过的更新安装包、日志和应用保存的图片，可以手动删除这个文件夹。

## 本地数据

EdgeTuck 仍将用户数据保存在兼容旧版本的本地数据目录：

```text
%LOCALAPPDATA%\DropShelf
```

常见文件和目录：

- `settings.json`：应用设置。
- `shelf.json`：暂存记录。
- `images\originals\`：应用保存的粘贴图片原图。
- `images\thumbs\`：图片缩略图。
- `updates\`：已下载的更新安装包。
- `logs\startup.log`：启动和未处理异常诊断日志。

## 开发

本仓库是基于 .NET 的 Windows WPF 应用。

在 WSL 中开发时，使用 Windows 侧 `dotnet` 命令：

```powershell
dotnet build DropShelf.sln
dotnet test DropShelf.sln --no-build
dotnet format DropShelf.sln --verify-no-changes --verbosity minimal
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
```

只有在需要验证安装包时才构建安装器：

```powershell
& "D:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\DropShelf.iss
```

正式发布应使用带签名和校验的打包脚本：

```powershell
$env:EDGE_TUCK_SIGN_CERT_PATH = "C:\path\to\certificate.pfx"
$env:EDGE_TUCK_SIGN_CERT_PASSWORD = "<certificate password>"
.\scripts\package-release.ps1
```

开发注意事项：

- `.trellis/`、`.agents/`、`.codex/` 和 `AGENTS.md` 是项目开发基础设施，需要保留。
- `artifacts/`、`installer/Output/`、`bin/`、`obj/`、`.trellis/.runtime/` 和 `.trellis/workspace/` 是生成物或本地运行时输出。
- 不要使用 `git add .`；只暂存当前任务相关文件。

## 项目文档

- 开发架构：[docs/architecture.md](docs/architecture.md)
- 功能路线图：[docs/feature-tasks.md](docs/feature-tasks.md)
- 功能开发契约：[docs/features/README.md](docs/features/README.md)
- 打包和发布流程：[docs/packaging.md](docs/packaging.md)
- Trellis 项目工作流：[.trellis/workflow.md](.trellis/workflow.md)
