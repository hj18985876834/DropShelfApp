# brainstorm: Windows 日常轻量软件方向

## Goal

探索并收敛一个适合 Windows 日常高频使用、功能轻量、可以打包分享给他人安装的软件方向。当前优先方向是“桌面暂存架”：一个用于跨应用临时暂存文件、图片、文本和链接的小工具。

## What I already know

* 用户希望开发 Windows 日常软件。
* 软件应适合经常使用，而不是一次性工具。
* 功能不需要复杂，偏轻量。
* 目标包含分享给其他人安装使用，因此需要考虑安装、更新、易用性和非开发者体验。
* 当前阶段是方向选择和需求探索，尚未决定具体产品。
* 用户提出了一个具体方向：简易硬件监控悬浮窗，显示 CPU、内存、磁盘占用、网速，支持半透明悬浮、拖动、透明度设置和阈值变色提醒。
* 用户认为硬件监控悬浮窗与任务管理器心智重叠，前面几个方向也不够满意。
* 用户新的筛选标准：轻量、易实现、用户体验好，并且不要和 Windows 自带功能重复。
* 用户喜欢“桌面暂存架”方向，并希望继续调研探索。
* 用户确认 MVP 功能基本可行，但要求除了功能以外，还要考虑用户体验效果、界面风格样式等方面。

## Assumptions (temporary)

* 优先考虑个人开发者可以完成的桌面应用，而不是大型平台型产品。
* 优先选择无需云服务、账号体系、复杂后端的本地优先工具。
* 首个版本应能在 Windows 10/11 上运行。
* 打包方式应尽量降低用户安装门槛。
* 如果选择硬件监控悬浮窗，V1 应坚持“无需驱动”，只读取 Windows 系统可提供的常规性能数据。
* 桌面暂存架已收敛为本地化软件：不依赖云服务、账号、外部 API 或联网能力。

## Open Questions

* Final user confirmation before moving from planning to implementation.

## Requirements (evolving)

* 产出 5-8 个可选软件方向。
* 每个方向说明目标用户、核心场景、MVP 功能、差异化点、开发复杂度和分享安装难度。
* 推荐 1-2 个最适合个人开发起步的方向。
* 优先考虑本地优先、无需账号、无需后端的 Windows 桌面工具。
* 候选方向应尽量落在日常高频小摩擦：剪贴板、下载目录整理、文本片段、文件动作、截图整理、专注记录、局域网传输。
* 如果选择硬件监控悬浮窗，MVP 应包含：
  * 实时显示 CPU、内存、磁盘、上传/下载网速。
  * 半透明悬浮窗口，可拖动，可调整透明度。
  * 超过阈值后变色提醒。
  * 不依赖内核驱动、账号、云服务或后台服务。
* 新方向筛选标准：
  * 不直接替代 Windows 自带应用。
  * 不正面竞争 PowerToys 已覆盖的通用系统增强功能。
  * 优先解决一个小而完整的工作流。
  * MVP 应能用 3-5 个功能形成闭环。
* 桌面暂存架 V1 应解决“跨应用移动东西时，需要临时放一下”的场景。
* 桌面暂存架 V1 采用“极简原生暂存架”路线。
* 桌面暂存架 V1 应优先支持：
  * 文件/文件夹拖入。
  * 文件/文件夹暂存只保存原路径引用，不复制文件到应用缓存。
  * 文本、URL、图片通过拖入或粘贴进入暂存架。
  * 临时复制/粘贴进来的图片保存到应用本地数据目录，并在中转界面显示缩略图。
  * 暂存项卡片化展示。
  * 从暂存架拖出或复制内容。
  * 基础动作集：拖出、复制、打开、定位到资源管理器、删除单项、清空全部。
  * 应用退出/重启后保留上次暂存内容。
  * 用户可以删除单个暂存项，也可以清空全部暂存项。
  * 屏幕边缘常驻小把手，点击或鼠标靠近后展开暂存架。
  * 停靠位置可选左、右、上、下；首次默认右侧。
  * 托盘显示/隐藏。
  * 支持开机自启动设置，但默认关闭。
* 桌面暂存架 V1 应保持本地优先，不做账号、云同步、云分享、联网依赖或外部服务调用。
* 桌面暂存架 V1 应包含明确的用户体验、视觉风格、动效、状态和可用性要求，而不仅是功能清单。

## Acceptance Criteria (evolving)

* [ ] 给出多个 Windows 日常轻量软件方向。
* [ ] 每个方向都能用 1-3 个核心功能形成可交付 MVP。
* [ ] 明确哪些方向适合第一版开发，哪些暂时不建议。
* [ ] 用户选择方向后，可以继续细化为完整 PRD。
* [ ] 硬件监控悬浮窗方向明确 V1 包含和不包含的监控指标。
* [ ] 桌面暂存架方向明确 MVP 输入类型、输出动作、窗口交互和不做范围。
* [ ] 桌面暂存架明确采用极简本地化路线。
* [ ] 桌面暂存架默认通过屏幕边缘常驻小把手唤起。
* [ ] 桌面暂存架支持用户选择左、右、上、下停靠位置。
* [ ] 桌面暂存架文件暂存采用路径引用语义，不自动复制或移动原文件。
* [ ] 桌面暂存架退出/重启后保留上次暂存项。
* [ ] 桌面暂存架支持单项删除和清空全部，避免长期堆积。
* [ ] 桌面暂存架 V1 不做自动过期或自动删除，避免用户误解。
* [ ] 桌面暂存架 V1 支持文件/文件夹、文本/URL、图片三类输入。
* [ ] 桌面暂存架 V1 对无稳定路径的图片保存本地副本，并生成缩略图展示。
* [ ] 桌面暂存架 V1 输出动作限定为基础动作集，不做显示名重命名或复杂右键扩展。
* [ ] 桌面暂存架拖出文件/图片到目标位置时默认复制，不移动原内容。
* [ ] 桌面暂存架支持开机自启动设置，默认关闭。
* [ ] 桌面暂存架定义可检查的 UX / UI 质量标准。
* [ ] 技术设计包含单元测试、文件系统集成测试、手动功能测试和安装包测试策略。
* [ ] 每个实现里程碑都能通过 `dotnet test` 和对应手动功能测试清单验收。
* [ ] 实现前确认 Windows 侧开发环境可构建和运行 WPF 应用。

## Definition of Done (team quality bar)

* Tests added/updated (unit/integration where appropriate)
* Lint / typecheck / CI green
* Docs/notes updated if behavior changes
* Rollout/rollback considered if risky

## Out of Scope (explicit)

* 本阶段不直接实现软件。
* 不设计需要账号、支付、团队协作、复杂云同步的重型产品。
* 不优先考虑移动端、Web SaaS 或浏览器插件，除非后续明确选择。
* 如果选择硬件监控悬浮窗，V1 不做 CPU/GPU 温度、风扇转速、电压、超频、硬件控制、完整任务管理器替代品。
* 桌面暂存架 V1 不做云上传/分享链接、OCR、图片处理、文件转换、剪贴板历史替代、跨设备同步、复杂多工作区、永久文件库。
* 桌面暂存架 V1 不做任何依赖互联网的能力，包括账号登录、远程存储、云端分享、自动同步、在线模板、在线分析。

## Research References

* [`research/windows-lightweight-app-directions.md`](research/windows-lightweight-app-directions.md) — Windows 轻量工具方向、同类工具模式、桌面技术栈取舍。
* [`research/hardware-monitor-floating-window.md`](research/hardware-monitor-floating-window.md) — 简易硬件监控悬浮窗可行性、Windows API 数据源和 MVP 边界。
* [`research/avoid-windows-built-in-overlap.md`](research/avoid-windows-built-in-overlap.md) — 避开 Windows 自带能力和 PowerToys 重叠后的新候选方向。
* [`research/desktop-drop-shelf-deep-dive.md`](research/desktop-drop-shelf-deep-dive.md) — 桌面暂存架竞品、MVP、技术栈、UX 风险和差异化路线。
* [`research/desktop-drop-shelf-ux-style.md`](research/desktop-drop-shelf-ux-style.md) — 桌面暂存架的 Windows/Fluent 风格、交互手感、动效和状态设计建议。
* [`research/desktop-drop-shelf-technical-route.md`](research/desktop-drop-shelf-technical-route.md) — WPF/.NET 技术路线、项目结构、模块边界、存储和打包建议。
* [`research/visual-motion-optimization.md`](research/visual-motion-optimization.md) — DropShelf 视觉、动效、拖放反馈和卡片体验的下一阶段优化建议。

## Research Notes

### What similar tools do

* Windows 上高频工具通常解决系统默认能力不够顺手的小摩擦，例如 PowerToys 的快捷工具、剪贴板增强、文件搜索/动作、截图/OCR、局域网传输。
* 成功方向往往不是“大而全效率平台”，而是一个日常重复动作的低摩擦入口。

### Feasible approaches here

**Approach A: Smart Clipboard Cleaner** (Recommended)

* How it works: 全局快捷键打开小面板，对当前剪贴板内容做清洗、格式转换、历史搜索和固定。
* Pros: 高频、容易解释、MVP 小、安装分享难度低。
* Cons: 后续若做完整剪贴板管理器，会涉及隐私、存储和窗口输入细节。

**Approach B: Download Folder Butler** (Recommended)

* How it works: 扫描 Downloads，按类型/时间/大小分组，预览后执行整理、删除重复、归档安装包。
* Pros: 痛点明确、演示效果好、安全边界清晰。
* Cons: 文件操作必须特别重视预览、撤销/回收站和误删防护。

**Approach C: Quick Text Snippet Launcher**

* How it works: 全局快捷键搜索常用文本片段并插入当前应用，支持日期/占位符。
* Pros: 粘性强，适合客服、办公、开发、学生等人群。
* Cons: 插入到当前应用的 Windows 输入兼容性会带来实现风险。

**Approach D: Mini File Action Palette**

* How it works: 搜索最近文件/下载文件，并提供复制路径、打开目录、重命名、移动、打开终端等动作。
* Pros: 比单纯文件搜索更聚焦“找到之后做什么”。
* Cons: 容易和成熟搜索工具重叠，需要明确差异化。

**Approach E: Screenshot-to-Task Card**

* How it works: 截图或读取剪贴板图片，快速加标题/标签并保存为本地可搜索卡片。
* Pros: 比截图编辑器简单，比桌面堆图片更有组织。
* Cons: OCR、搜索和图片管理会逐步扩大范围。

**Approach F: Local Focus Timer + Notes**

* How it works: 托盘里的专注计时器，每次专注附带简短笔记和本地日报。
* Pros: 实现简单、安装轻。
* Cons: 市场拥挤，必须有明确的小差异点。

**Approach G: LAN Drop Zone**

* How it works: 局域网内发送文本/文件，不走云、不需要账号。
* Pros: 产品故事清楚，隐私友好。
* Cons: 跨设备发现、网络异常、防火墙和跨平台预期会提高复杂度。

**Approach H: Hardware Monitor Floating Window** (New Recommended Candidate)

* How it works: 常驻半透明悬浮窗显示 CPU、内存、磁盘活动、上传/下载网速，支持拖动、透明度、阈值变色和托盘控制。
* Pros: 价值直观、日常高频、Windows 用户容易理解，不需要账号/云服务，MVP 可以很轻。
* Cons: 必须严格控制范围；温度、风扇、GPU 深度数据会显著提高技术和支持复杂度。

**Approach I: Desktop Drop Shelf** (New Top Recommendation)

* How it works: 屏幕边缘有一个小暂存架，用户可以把文件、截图、链接、文本拖进去，稍后再拖出、复制、打开、定位或清除。
* Pros: 不替代 Windows 自带应用，而是补足跨应用临时中转流程；轻量、可视化、容易演示，体验空间大。
* Cons: 拖拽体验必须足够顺滑；需要处理文件、文本、图片、URL 等不同数据类型。
* Current MVP recommendation: 先做极简原生暂存架，文件/文件夹优先，文本/URL/图片粘贴其次，只保留复制、打开、定位、移除、清空等基础动作。

**Approach J: Battery Care Notifier**

* How it works: 托盘应用提醒用户在电量高于/低于阈值时插拔电源，例如 80% 断电、25% 充电。
* Pros: 极易实现，不和 Windows 电池显示重复，因为重点是自定义提醒。
* Cons: 只适合笔记本用户，且不能承诺真实限制充电。

**Approach K: App Audio Profile Switcher**

* How it works: 保存不同场景下的应用音量/静音状态，例如会议、游戏、录屏，一键切换。
* Pros: Windows 有音量混合器，但没有好用的场景配置切换。
* Cons: Windows 音频会话 API 有边界情况，实现难度中等。

**Approach L: Share-Ready File Packager**

* How it works: 拖入文件后按预设重命名、压缩、图片缩放、清理元数据并生成分享包。
* Pros: Windows 只有基础 zip，不覆盖“分享前整理文件”的完整工作流。
* Cons: PDF/图片处理容易扩张范围，V1 需要克制。

**Approach M: Local QR Handoff**

* How it works: 把文本、链接或临时本地文件分享地址生成 QR，手机扫码拿走。
* Pros: 轻、直观，适合 PC 到手机的快速传递。
* Cons: 文件分享会遇到局域网、防火墙和过期控制问题。

## Rejected / Lower-Priority Directions

* Generic hardware monitor: task manager already covers the mental model; unless定位非常独特，否则差异化不足。
* Generic clipboard manager: Windows clipboard history and PowerToys/第三方工具覆盖较多。
* Generic screenshot tool: Snipping Tool and PowerToys 已覆盖截图、标注、OCR 等基础能力。
* Generic launcher/search tool: PowerToys Run、Command Palette、Windows 搜索和第三方搜索工具重叠明显。

## Product Direction Decision (Draft)

**Context**: 用户希望找到一个轻量、易实现、体验好且不与 Windows 自带功能重复的 Windows 桌面软件方向。

**Decision**: 优先探索“桌面暂存架”，并采用“极简原生暂存架”路线。V1 是完全本地化软件，补足跨应用临时中转流程，而不是替代剪贴板、文件管理器、截图工具或任务管理器。

**Consequences**:

* V1 成功关键是拖拽体验、窗口出现/隐藏方式和用户对“引用/复制/移动”的理解。
* 技术上更适合 Windows 原生桌面栈，例如 WPF 或 Windows App SDK / WinUI 3。
* 需要严格避免做成文件管理器、剪贴板历史或云分享工具。
* 本地化边界降低了隐私、账号、服务端、网络异常和运维复杂度，但也意味着 V1 不提供跨设备同步或云端分享。

## MVP Summary

### Goal

Build a local-only Windows desktop drop shelf that lets users temporarily hold files, folders, text, URLs, and images while moving between applications.

### Core User Flow

1. User opens the shelf from a persistent screen-edge handle.
2. User drags or pastes content into the shelf.
3. Shelf shows compact cards for each item.
4. User later drags an item out, copies it, opens it, reveals it in Explorer, removes it, or clears all items.
5. Shelf records persist across app restart until the user manually removes them.

### V1 Requirements

* Local-only Windows desktop app.
* Screen-edge persistent handle.
* Docking position selectable: left, right, top, bottom; first-run default right.
* Support file/folder items by storing original path references.
* Support text/URL items by storing local app records.
* Support pasted/dragged images by saving app-owned local copies and displaying thumbnails.
* Persist shelf records across app exit/restart.
* Support manual cleanup: remove item and clear all.
* Drag-out defaults to copy, not move.
* Basic actions only: drag out, copy, open, reveal in Explorer, remove item, clear all.
* Tray icon for show/hide and quit.
* Start with Windows setting supported, default off.
* Short settings window for dock edge, start with Windows, theme mode, and item density.
* V1 should be distributed as a traditional Windows installer.
* V1 installer should install for the current user only and avoid requiring administrator privileges.
* Start-with-Windows setting should use the current user's Registry `Run` key, managed by the app setting.
* V1 should not introduce a third-party MVVM framework; use small in-house `ObservableObject` and `RelayCommand` primitives.

### UX / UI Requirements

* Visual style should feel like a quiet Windows-native utility rather than a dashboard, launcher, or marketing app.
* Use Windows-native / Fluent-inspired style: neutral surfaces, system-like typography, subtle borders/shadows, restrained accent color, and system light/dark compatibility.
* Edge handle should be compact, discoverable, and low-distraction.
* Expanded shelf should feel lightweight and temporary, with a clear drop zone and compact item cards.
* Item cards should have stable dimensions and clear type affordances for file/folder, text/URL, and image items.
* Panel size should remain fixed in V1; item density can be compact or comfortable.
* Use icon buttons with tooltips for repeated card actions where practical.
* Item card action buttons should appear on hover to keep the default list visually quiet.
* Long filenames, paths, and text previews must truncate gracefully and expose full content through tooltip or detail affordance.
* The app should avoid decorative illustrations, large hero text, heavy gradients, strong color themes, and attention-grabbing animation.
* Motion should be functional and short: shelf expand/collapse, item add/remove, and drop accepted states.
* Clear/delete wording must make it obvious that clearing shelf records does not delete original files.
* Missing files should remain visible with a calm missing-state indicator and a remove action.
* Unsupported drops should give brief feedback without interrupting the workflow.
* Empty state should be minimal and useful: show the drop target and short instruction only.
* Basic keyboard ergonomics should be considered: Escape collapses shelf; Delete removes selected item; Ctrl+C copies selected item where practical.
* V1 should support a single selected card state for basic keyboard actions.

### V1 Non-Goals

* Account/login.
* Cloud sync or cloud sharing.
* External API or internet dependency.
* Clipboard history replacement.
* File manager replacement.
* Permanent file library.
* OCR, image editing, file conversion, compression.
* Automatic expiration or automatic deletion.
* Drag-detection auto-popup as primary invocation.
* Complex right-click extension actions.
* Decorative or playful UI theme.
* Heavy customization of colors, animations, card shapes, or layout density.

### Recommended Technical Approach

* Technical design is documented in [`info.md`](info.md).
* Recommended stack: C# + .NET 10 LTS + WPF + MVVM-lite.
* Keep V1 as one WPF app project plus one test project.
* Store app data under the user's local app data directory.
* Store file/folder records as paths, text/URL records as local metadata, and image copies/thumbnails in app-owned local folders.
* Recommended installer route: publish self-contained Windows x64 output and package it with Inno Setup as a traditional `setup.exe`.
* Installer scope: per-user install, preferably under `%LOCALAPPDATA%/Programs/DropShelf/`.
* Startup implementation: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, default off and app-managed.
* MVVM dependency: no third-party MVVM framework in V1; implement minimal in-house primitives.
* Testing strategy: MSTest + `dotnet test` for unit/integration-friendly logic, plus manual checklists for WPF UI, drag/drop, tray, installer, and persistence behavior.
* Development environment: WSL can be used for Trellis/docs/source editing, but WPF build/run/UI testing/installer packaging must happen on Windows.
* Windows prerequisites before implementation: .NET 10 SDK, Windows desktop development tooling, and Inno Setup.

### UX Surfaces

* Edge handle: small tab on selected edge, icon-only or nearly icon-only, hover state, tooltip.
* Shelf panel: compact panel with item count, clear-all action, collapse action, and scrollable item area.
* Empty shelf: simple drop zone with concise localizable instruction.
* Item card: icon/thumbnail, name/preview, secondary info, basic actions.
* Settings: short settings window for dock edge, start with Windows, theme mode, and item density.

### Visual Style Decision

* V1 style direction: Windows-native / Fluent-inspired.
* Avoid custom visual identity in V1 beyond a simple app icon and restrained accent usage.
* The app should feel like a dependable utility that belongs on Windows 11.
* Theme: follow system by default, with settings options for system / light / dark.

### Layout Density Decision

* V1 panel dimensions are fixed per dock orientation.
* User can choose compact or comfortable item density.
* V1 does not support freeform panel resizing.

### Settings Decision

* V1 includes a short settings window.
* Settings window can be opened from the tray menu and/or shelf panel settings button.
* V1 settings: dock edge, start with Windows, theme mode, item density.
* Avoid deep settings categories in V1.

### Empty State Decision

* V1 empty state: simple drop zone.
* Use a short localizable instruction such as "Drop files, text, or images here".
* Avoid first-run tutorials, illustrations, or multi-entry onboarding in V1.

### Card Action Decision

* Card action buttons appear on hover.
* Cards should still communicate type and draggability when actions are hidden.
* Repeated actions should use icons with tooltips where practical.

### Keyboard Decision

* V1 supports single-card selection.
* Keyboard actions:
  * Escape collapses shelf.
  * Delete removes the selected shelf record.
  * Ctrl+C copies selected item content or path.
  * Enter opens the selected item where applicable.

### Implementation Plan (small PRs)

* PR1: App shell, tray icon, edge handle, docking settings.
* PR2: Shelf item model, local persistence, file/folder drag-in and card UI.
* PR3: Text/URL/image paste/drag support, image local copy, thumbnails.
* PR4: Drag-out/copy/open/reveal/remove/clear actions.
* PR5: Polish, missing-file states, startup setting, packaging.

## Interaction Decisions

* Default invocation: screen-edge persistent handle. The shelf expands when the user clicks the handle or hovers near it.
* Docking position: user can choose left, right, top, or bottom edge. First-run default is right edge.
* Output actions for V1: drag out, copy, open, reveal in Explorer, remove item, clear all.
* Drag-out semantics: copy by default. The app should not move source files as part of normal shelf drag-out behavior.
* Startup behavior: support "start with Windows" as an opt-in setting. Default is off.
* Non-goal for V1: drag-detection auto-popup as the primary invocation model, because it adds compatibility risk.

## Data Decisions

* File/folder items are stored as references to original paths. The app does not copy, move, or duplicate files into an internal cache by default.
* If a referenced file/folder is deleted, moved, or becomes unavailable, the shelf should show a missing-item state and offer remove-from-shelf.
* Text/URL items are stored locally as app records.
* Image items without stable source paths are copied into the app's local data directory, with thumbnails generated for shelf display.
* The app should not rely on the OS temp directory for persisted shelf images because shelf records persist across app restart.
* Removing an image shelf record should remove the app-owned local image copy and thumbnail.
* Shelf records persist across app exit/restart.
* Users can remove individual records or clear all records.
* Clearing shelf records does not delete, move, or modify original files.
* V1 does not automatically expire, delete, or evict shelf records. Pile-up control is manual only.

## Technical Notes

* Trellis task created at `.trellis/tasks/07-04-windows-daily-light-app-ideas`.
* Project root is not a Git repository; Trellis still records planning artifacts under `.trellis/tasks/`.
* Research artifacts should be written under `research/` if external market/tooling research is performed.
* If implementation proceeds, likely stacks are Tauri for small web-UI desktop app, or Windows App SDK/WinUI 3 for native Windows-only integration.
* For the hardware monitor direction, Windows App SDK / WinUI 3 or WPF is likely a better first stack than Electron because native Windows APIs, tray behavior, topmost transparent windows, and low resource usage matter.
* For Desktop Drop Shelf, WPF may be the pragmatic first stack because drag/drop, tray behavior, topmost windows, and classic desktop integration are central to V1.
