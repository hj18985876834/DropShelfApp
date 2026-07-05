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
    string AboutTitle,
    string SoftwareLabel,
    string IntroductionLabel,
    string UsageLabel,
    string VersionLabel,
    string DeveloperLabel,
    string ContactLabel,
    string ApplyText,
    string CloseText,
    string AppDescription,
    string UsageGuide,
    string Developer,
    string SettingsSaved,
    string SettingsSaveFailed,
    string ShelfItemCountSuffix,
    string ClearAllTooltip,
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
    string ContextRemove,
    string MissingSource,
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
    string DragOutTooLarge,
    string DragOutSizeUnreadable,
    string DragOutSourceMissing)
{
    public static AppText Chinese { get; } = new(
        "DropShelf 设置",
        "设置",
        "偏好设置",
        "收纳栏位置",
        "重置到右侧边缘",
        "主题",
        "密度",
        "语言",
        "开机自启动",
        "关于",
        "软件",
        "介绍",
        "使用方法",
        "版本",
        "开发者",
        "联系方式",
        "应用",
        "关闭",
        "这是由江江学长开发的一款运行于 Windows 本地桌面的临时收纳栏工具，可存放文件、文件夹、文本、链接与图片。",
        "将内容拖放到屏幕边缘手柄或打开后的收纳栏中，需要时可复制、打开、在资源管理器中定位、移除，或再拖回其他窗口使用。",
        "江江学长",
        "设置已保存。",
        "无法保存设置。",
        " 项",
        "清空全部",
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
        "移除",
        "源文件缺失",
        "显示或隐藏 DropShelf",
        "显示收纳栏",
        "隐藏收纳栏",
        "设置",
        "退出",
        "清空 DropShelf",
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
        "项目过大，无法拖出。最大支持 512 MB。",
        "无法读取项目大小。",
        "源文件缺失。");

    public static AppText English { get; } = new(
        "DropShelf Settings",
        "Settings",
        "Preferences",
        "Shelf position",
        "Reset to right edge",
        "Theme",
        "Density",
        "Language",
        "Start with Windows",
        "About",
        "Software",
        "Introduction",
        "How to use",
        "Version",
        "Developer",
        "Contact",
        "Apply",
        "Close",
        "A local Windows desktop shelf developed by Jiangjiang Xuezhang for temporarily storing files, folders, text, links, and images.",
        "Drag content onto the screen-edge handle or open shelf, then copy, open, reveal, remove, or drag items back out when needed.",
        "Jiangjiang Xuezhang",
        "Settings saved.",
        "Unable to save settings.",
        " items",
        "Clear all",
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
        "Remove",
        "Source missing",
        "Show or hide DropShelf",
        "Show Shelf",
        "Hide Shelf",
        "Settings",
        "Quit",
        "Clear DropShelf",
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
        "Item is too large to drag out. Maximum supported size is 512 MB.",
        "Unable to read item size.",
        "Source is missing.");
}
