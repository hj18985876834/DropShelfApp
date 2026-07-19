using System.Globalization;
using DropShelf.App.Models;

namespace DropShelf.App.Services;

public sealed class LocalizationService
{
    public LocalizationService(LanguageMode languageMode = LanguageMode.Chinese)
    {
        LanguageMode = languageMode;
    }

    public event EventHandler? LanguageChanged;

    public LanguageMode LanguageMode { get; private set; }

    public bool IsChinese => LanguageMode == LanguageMode.Chinese;

    public AppText Text => IsChinese ? AppText.Chinese : AppText.English;

    public void SetLanguage(LanguageMode languageMode)
    {
        if (LanguageMode == languageMode)
        {
            return;
        }

        LanguageMode = languageMode;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string LanguageName(LanguageMode value)
    {
        return value switch
        {
            LanguageMode.Chinese => IsChinese ? "中文" : "Chinese",
            LanguageMode.English => IsChinese ? "英文" : "English",
            _ => value.ToString(),
        };
    }

    public string ThemeName(ThemeMode value)
    {
        return value switch
        {
            ThemeMode.System => IsChinese ? "跟随系统" : "System",
            ThemeMode.Light => IsChinese ? "浅色" : "Light",
            ThemeMode.Dark => IsChinese ? "深色" : "Dark",
            _ => value.ToString(),
        };
    }

    public string DensityName(DensityMode value)
    {
        return value switch
        {
            DensityMode.Compact => IsChinese ? "紧凑" : "Compact",
            DensityMode.Comfortable => IsChinese ? "舒适" : "Comfortable",
            _ => value.ToString(),
        };
    }

    public string AutoUpdateCheckModeName(AutoUpdateCheckMode value)
    {
        return value switch
        {
            AutoUpdateCheckMode.Never => Text.AutoUpdateNever,
            AutoUpdateCheckMode.Daily => Text.AutoUpdateDaily,
            AutoUpdateCheckMode.Weekly => Text.AutoUpdateWeekly,
            _ => value.ToString(),
        };
    }

    public string TypeDisplayName(ShelfItemType type)
    {
        return type switch
        {
            ShelfItemType.File => Text.TypeFile,
            ShelfItemType.Folder => Text.TypeFolder,
            ShelfItemType.Text => Text.TypeText,
            ShelfItemType.Url => Text.TypeUrl,
            ShelfItemType.Image => Text.TypeImage,
            _ => Text.TypeItem,
        };
    }

    public string MetadataText(ShelfItemType type, string detail, DateTimeOffset createdAt)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{TypeDisplayName(type)} · {detail} · {createdAt.LocalDateTime:MM-dd HH:mm}");
    }

    public string TextLengthDetail(int length)
    {
        if (length == 0)
        {
            return Text.EmptyText;
        }

        return IsChinese ? $"{length} 字符" : $"{length} chars";
    }

    public string ClearAllMessage(int itemCount)
    {
        return IsChinese
            ? $"确定要清空当前 {itemCount} 个暂存项吗？原始文件不会被删除。"
            : $"Clear {itemCount} shelf item{(itemCount == 1 ? string.Empty : "s")}? Original files will not be deleted.";
    }

    public string AddItemsMessage(int addedCount, int duplicateCount)
    {
        if (duplicateCount == 0)
        {
            return IsChinese
                ? $"已添加 {addedCount} 项。"
                : $"Added {addedCount} item{(addedCount == 1 ? string.Empty : "s")}.";
        }

        if (addedCount == 0)
        {
            return IsChinese
                ? $"已跳过 {duplicateCount} 个重复路径。"
                : $"Skipped {duplicateCount} duplicate path{(duplicateCount == 1 ? string.Empty : "s")}.";
        }

        return IsChinese
            ? $"已添加 {addedCount} 项，跳过 {duplicateCount} 个重复路径。"
            : $"Added {addedCount} item{(addedCount == 1 ? string.Empty : "s")}, skipped {duplicateCount} duplicate path{(duplicateCount == 1 ? string.Empty : "s")}.";
    }

    public string AddItemsMessage(IEnumerable<ShelfItem> addedItems, int duplicateCount)
    {
        ArgumentNullException.ThrowIfNull(addedItems);

        var items = addedItems.ToArray();
        if (items.Length == 0)
        {
            return AddItemsMessage(0, duplicateCount);
        }

        var summary = FormatItemTypeSummary(items);
        if (duplicateCount == 0)
        {
            return IsChinese
                ? $"已添加 {summary}。"
                : $"Added {summary}.";
        }

        return IsChinese
            ? $"已添加 {summary}，跳过 {duplicateCount} 个重复路径。"
            : $"Added {summary}, skipped {duplicateCount} duplicate path{(duplicateCount == 1 ? string.Empty : "s")}.";
    }

    private string FormatItemTypeSummary(IReadOnlyCollection<ShelfItem> items)
    {
        var parts = items
            .GroupBy(item => item.Type)
            .OrderBy(group => ItemTypeSortOrder(group.Key))
            .Select(group => FormatItemTypeCount(group.Key, group.Count()))
            .ToArray();

        return string.Join(IsChinese ? "、" : ", ", parts);
    }

    private string FormatItemTypeCount(ShelfItemType type, int count)
    {
        if (IsChinese)
        {
            return type switch
            {
                ShelfItemType.File => $"{count} 个文件",
                ShelfItemType.Folder => $"{count} 个文件夹",
                ShelfItemType.Image => $"{count} 张图片",
                ShelfItemType.Text => $"{count} 条文本",
                ShelfItemType.Url => $"{count} 个链接",
                _ => $"{count} 个项目",
            };
        }

        return type switch
        {
            ShelfItemType.File => $"{count} file{(count == 1 ? string.Empty : "s")}",
            ShelfItemType.Folder => $"{count} folder{(count == 1 ? string.Empty : "s")}",
            ShelfItemType.Image => $"{count} image{(count == 1 ? string.Empty : "s")}",
            ShelfItemType.Text => $"{count} text item{(count == 1 ? string.Empty : "s")}",
            ShelfItemType.Url => $"{count} link{(count == 1 ? string.Empty : "s")}",
            _ => $"{count} item{(count == 1 ? string.Empty : "s")}",
        };
    }

    private static int ItemTypeSortOrder(ShelfItemType type)
    {
        return type switch
        {
            ShelfItemType.File => 0,
            ShelfItemType.Folder => 1,
            ShelfItemType.Image => 2,
            ShelfItemType.Text => 3,
            ShelfItemType.Url => 4,
            _ => 5,
        };
    }

    public string ClearInvalidMessage(int itemCount)
    {
        return IsChinese
            ? $"已清理 {itemCount} 条无效记录。"
            : $"Cleared {itemCount} invalid record{(itemCount == 1 ? string.Empty : "s")}.";
    }

    public string RemoveSelectedMessage(int itemCount)
    {
        return IsChinese
            ? $"确定要移除选中的 {itemCount} 个暂存项吗？原始文件不会被删除。"
            : $"Remove {itemCount} selected shelf items? Original files will not be deleted.";
    }

    public string CopySelectedMessage(int itemCount)
    {
        return IsChinese
            ? $"已复制 {itemCount} 个暂存项。"
            : $"Copied {itemCount} shelf item{(itemCount == 1 ? string.Empty : "s")}.";
    }

    public string CopySelectedMessage(ShelfItemType type, int itemCount)
    {
        if (IsChinese)
        {
            return type switch
            {
                ShelfItemType.File => $"已复制 {itemCount} 个文件。",
                ShelfItemType.Folder => $"已复制 {itemCount} 个文件夹。",
                ShelfItemType.Image => $"已复制 {itemCount} 张图片。",
                ShelfItemType.Text => $"已复制 {itemCount} 条文本。",
                ShelfItemType.Url => $"已复制 {itemCount} 个链接。",
                _ => CopySelectedMessage(itemCount),
            };
        }

        return type switch
        {
            ShelfItemType.File => $"Copied {itemCount} file{(itemCount == 1 ? string.Empty : "s")}.",
            ShelfItemType.Folder => $"Copied {itemCount} folder{(itemCount == 1 ? string.Empty : "s")}.",
            ShelfItemType.Image => $"Copied {itemCount} image{(itemCount == 1 ? string.Empty : "s")}.",
            ShelfItemType.Text => $"Copied {itemCount} text item{(itemCount == 1 ? string.Empty : "s")}.",
            ShelfItemType.Url => $"Copied {itemCount} link{(itemCount == 1 ? string.Empty : "s")}.",
            _ => CopySelectedMessage(itemCount),
        };
    }

}

public sealed record AppText(
    string SettingsWindowTitle,
    string SettingsHeaderTitle,
    string PreferencesTitle,
    string ShelfPositionLabel,
    string ResetDockPositionText,
    string ThemeLabel,
    string DensityLabel,
    string LanguageLabel,
    string StartWithWindowsText,
    string AutoUpdateCheckLabel,
    string AboutTitle,
    string SoftwareLabel,
    string IntroductionLabel,
    string UsageLabel,
    string VersionLabel,
    string UpdatesTitle,
    string CheckForUpdatesText,
    string DownloadUpdateText,
    string CheckingForUpdates,
    string NoUpdateAvailable,
    string UpdateAvailableFormat,
    string DownloadingUpdate,
    string DownloadProgressFormat,
    string UpdateFailed,
    string UpdateVersionFormat,
    string UpdateReleaseDateFormat,
    string UpdateSizeFormat,
    string UpdateSha256Format,
    string MandatoryUpdateText,
    string OptionalUpdateText,
    string UpdateInstallConfirmTitle,
    string UpdateInstallConfirmMessageFormat,
    string UpdateInstallCancelled,
    string UpdateDownloadedVerifiedFormat,
    string NoReleaseNotesText,
    string AutoUpdateNever,
    string AutoUpdateDaily,
    string AutoUpdateWeekly,
    string AutomaticUpdateAvailableTitle,
    string AutomaticUpdateAvailableMessageFormat,
    string UpdateCompletedTitle,
    string UpdateCompletedMessageFormat,
    string HotkeyRegistrationFailedTitle,
    string HotkeyRegistrationFailedMessage,
    string QuickPasteTitle,
    string QuickPasteUnsupported,
    string QuickPasteFailed,
    string DeveloperLabel,
    string ContactLabel,
    string ApplyText,
    string CloseText,
    string UsageGuide,
    string Developer,
    string SettingsSaved,
    string SettingsSaveFailed,
    string ShelfItemCountSuffix,
    string FilterLabel,
    string SearchPlaceholder,
    string FilterAll,
    string FilterFiles,
    string FilterFolders,
    string FilterText,
    string FilterLinks,
    string FilterImages,
    string NoResultsTitle,
    string NoResultsDescription,
    string PinShelfTooltip,
    string UnpinShelfTooltip,
    string ClearAllTooltip,
    string ClearInvalidTooltip,
    string SettingsTooltip,
    string CollapseTooltip,
    string EmptyTitle,
    string EmptyDescription,
    string EmptyFileChip,
    string EmptyTextChip,
    string EmptyImageChip,
    string ReleaseToAdd,
    string UnsupportedContent,
    string DragOutTooltip,
    string ContextCopy,
    string ContextOpen,
    string ContextReveal,
    string ContextRelink,
    string ContextRemove,
    string MissingSource,
    string MissingSourceAction,
    string DuplicateSource,
    string HandleTooltip,
    string TrayShowShelf,
    string TrayHideShelf,
    string TraySettings,
    string TrayQuit,
    string ClearAllTitle,
    string UntitledItem,
    string TypeFile,
    string TypeFolder,
    string TypeText,
    string TypeUrl,
    string TypeImage,
    string TypeItem,
    string FolderDetail,
    string MissingDetail,
    string UnknownSize,
    string EmptyText,
    string UrlDetail,
    string CopyNoContent,
    string CopyFailed,
    string PathCopied,
    string Copied,
    string UrlOpenFailed,
    string NoPath,
    string OpenFailed,
    string MissingImage,
    string ImageCopyFailed,
    string RevealFailed,
    string RelinkDuplicate,
    string RelinkInvalidPath,
    string RelinkUpdated,
    string DragOutTooLarge,
    string DragOutSizeUnreadable,
    string DragOutSourceMissing,
    string MultiCopyMixedTypes,
    string MultiCopyNoContent,
    string ReorderHandleTooltip)
{
    public static AppText Chinese { get; } = new(
        "EdgeTuck 设置",
        "设置",
        "偏好设置",
        "收纳栏位置",
        "重置到右侧边缘",
        "主题",
        "密度",
        "语言",
        "开机自启动",
        "自动检查更新",
        "关于",
        "软件",
        "介绍",
        "使用方法",
        "版本",
        "更新",
        "检查更新",
        "下载并安装",
        "正在检查更新...",
        "当前已是最新版本。",
        "发现新版本 {0}。",
        "正在下载更新...",
        "正在下载更新 {0}%",
        "检查或下载更新失败，请稍后重试。",
        "版本：{0}",
        "发布日期：{0}",
        "安装包大小：{0}",
        "SHA-256：{0}",
        "此更新标记为必需更新。",
        "此更新为可选更新。",
        "安装更新",
        "即将下载并安装 EdgeTuck {0}。\n\n更新说明：\n{1}\n\n下载完成后会校验 SHA-256，校验通过才会启动安装器。是否继续？",
        "已取消安装更新。",
        "EdgeTuck {0} 安装包已通过 SHA-256 校验，正在启动安装器。",
        "暂无更新说明。",
        "从不",
        "每天",
        "每周",
        "EdgeTuck 有可用更新",
        "发现新版本 {0}。请打开设置查看更新说明并手动安装。",
        "EdgeTuck 已更新",
        "已更新到版本 {0}。",
        "快捷键不可用",
        "部分全局快捷键已被其他应用占用，EdgeTuck 会继续运行。",
        "已快速加入",
        "当前剪贴板没有可加入的内容。",
        "无法加入剪贴板内容。",
        "开发者",
        "联系方式",
        "应用",
        "关闭",
        "将内容拖放到屏幕边缘手柄或打开后的收纳栏中，需要时可复制、打开、在资源管理器中定位、移除，或再拖回其他窗口使用。",
        "江江学长",
        "设置已保存。",
        "无法保存设置。",
        " 项",
        "筛选",
        "搜索卡片",
        "全部",
        "文件",
        "文件夹",
        "文本",
        "链接",
        "图片",
        "没有匹配项",
        "当前筛选下没有暂存项。",
        "固定收纳栏",
        "取消固定收纳栏",
        "清空全部",
        "清理缺失记录",
        "设置",
        "折叠",
        "暂存架为空",
        "拖入文件、文本或图片，稍后再取用。",
        "文件",
        "文本",
        "图片",
        "松开即可添加",
        "暂不支持此内容",
        "拖出以复制",
        "复制",
        "打开",
        "在资源管理器中定位",
        "重新关联...",
        "移除",
        "源文件缺失",
        "源路径不可用，可重新关联或移除",
        "重复路径",
        "显示或隐藏 EdgeTuck",
        "显示收纳栏",
        "隐藏收纳栏",
        "设置",
        "退出",
        "清空 EdgeTuck",
        "未命名项目",
        "文件",
        "文件夹",
        "文本",
        "链接",
        "图片",
        "项目",
        "文件夹",
        "已缺失",
        "大小未知",
        "空文本",
        "链接",
        "没有可复制的内容。",
        "复制失败。",
        "路径已复制。",
        "已复制。",
        "链接无法打开。",
        "没有可用路径。",
        "项目缺失或无法打开。",
        "图片缺失。",
        "图片复制失败。",
        "项目缺失或无法定位。",
        "该路径已在收纳栏中。",
        "请选择匹配类型的有效路径。",
        "已重新关联。",
        "项目过大，无法拖出。最大支持 512 MB。",
        "无法读取项目大小。",
        "源文件缺失。",
        "请选择同一类型的暂存项后再复制。",
        "选中的暂存项没有可复制的内容。",
        "拖动此区域可排序");

    public static AppText English { get; } = new(
        "EdgeTuck Settings",
        "Settings",
        "Preferences",
        "Shelf position",
        "Reset to right edge",
        "Theme",
        "Density",
        "Language",
        "Start with Windows",
        "Automatically check for updates",
        "About",
        "Software",
        "Introduction",
        "How to use",
        "Version",
        "Updates",
        "Check for updates",
        "Download and install",
        "Checking for updates...",
        "You are using the latest version.",
        "Version {0} is available.",
        "Downloading update...",
        "Downloading update {0}%",
        "Unable to check for or download updates. Please try again later.",
        "Version: {0}",
        "Release date: {0}",
        "Installer size: {0}",
        "SHA-256: {0}",
        "This update is marked as mandatory.",
        "This update is optional.",
        "Install update",
        "EdgeTuck {0} will be downloaded and installed.\n\nRelease notes:\n{1}\n\nThe installer will be launched only after SHA-256 verification succeeds. Continue?",
        "Update installation was cancelled.",
        "EdgeTuck {0} installer passed SHA-256 verification. Launching installer.",
        "No release notes.",
        "Never",
        "Daily",
        "Weekly",
        "EdgeTuck update available",
        "Version {0} is available. Open Settings to review notes and install it manually.",
        "EdgeTuck updated",
        "Updated to version {0}.",
        "Shortcuts unavailable",
        "Some global shortcuts are already used by another app. EdgeTuck will keep running.",
        "Quick paste added",
        "The current clipboard content cannot be added.",
        "Unable to add clipboard content.",
        "Developer",
        "Contact",
        "Apply",
        "Close",
        "Drag content onto the screen-edge handle or open shelf, then copy, open, reveal, remove, or drag items back out when needed.",
        "Jiangjiang Xuezhang",
        "Settings saved.",
        "Unable to save settings.",
        " items",
        "Filter",
        "Search cards",
        "All",
        "Files",
        "Folders",
        "Text",
        "Links",
        "Images",
        "No matching items",
        "No shelf items match this filter.",
        "Pin shelf",
        "Unpin shelf",
        "Clear all",
        "Clear missing records",
        "Settings",
        "Collapse",
        "Shelf is empty",
        "Drop files, text, or images here and pick them up later.",
        "Files",
        "Text",
        "Images",
        "Release to add",
        "Unsupported content",
        "Drag out to copy",
        "Copy",
        "Open",
        "Reveal in Explorer",
        "Relink...",
        "Remove",
        "Source missing",
        "Source path is unavailable. Relink or remove it.",
        "Duplicate path",
        "Show or hide EdgeTuck",
        "Show Shelf",
        "Hide Shelf",
        "Settings",
        "Quit",
        "Clear EdgeTuck",
        "Untitled item",
        "File",
        "Folder",
        "Text",
        "Link",
        "Image",
        "Item",
        "folder",
        "missing",
        "size unknown",
        "empty text",
        "link",
        "No content to copy.",
        "Copy failed.",
        "Path copied.",
        "Copied.",
        "Unable to open link.",
        "No available path.",
        "Item is missing or cannot be opened.",
        "Image is missing.",
        "Image copy failed.",
        "Item is missing or cannot be revealed.",
        "That path is already on the shelf.",
        "Choose a valid path with the same type.",
        "Relinked.",
        "Item is too large to drag out. Maximum supported size is 512 MB.",
        "Unable to read item size.",
        "Source is missing.",
        "Select shelf items of the same type before copying.",
        "The selected shelf items have no copyable content.",
        "Drag here to reorder");
}
