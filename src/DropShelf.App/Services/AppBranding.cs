using System.Text.Json.Serialization;

namespace DropShelf.App.Services;

public sealed class AppBranding
{
    public const string DefaultDisplayName = "EdgeTuck";

    public static AppBranding Default { get; } = new()
    {
        DisplayName = DefaultDisplayName,
        Descriptions = new LocalizedText
        {
            Chinese = "EdgeTuck 是一款贴边常驻的 Windows 临时收纳工具，适合在整理文件、收集素材、搬运文本链接或处理中途截图时，把内容先放到屏幕边缘，稍后再复制、打开、定位、移除或拖回其它窗口。所有数据保存在本机，文件和文件夹仅记录原始路径，粘贴图片保存到本地应用数据目录，不需要账号，也不会上传到云端。",
            English = "EdgeTuck is a local Windows edge shelf for temporarily holding files, folders, text, links, and images while you organize work, collect references, move snippets, or handle screenshots. It keeps data on this PC, stores files and folders as original path references, saves pasted images under local app data, and does not require an account or cloud service.",
        },
    };

    public string? DisplayName { get; init; }

    public LocalizedText Descriptions { get; init; } = new();

    public string DisplayNameOrDefault()
    {
        return string.IsNullOrWhiteSpace(DisplayName)
            ? DefaultDisplayName
            : DisplayName;
    }

    public string DescriptionFor(bool useChinese)
    {
        return Descriptions.ValueFor(useChinese, Default.Descriptions);
    }
}

public sealed class LocalizedText
{
    [JsonPropertyName("zh-CN")]
    public string? Chinese { get; init; }

    [JsonPropertyName("en-US")]
    public string? English { get; init; }

    public string ValueFor(bool useChinese, LocalizedText fallback)
    {
        return useChinese
            ? FirstNonEmpty(Chinese, English, fallback.Chinese, fallback.English)
            : FirstNonEmpty(English, Chinese, fallback.English, fallback.Chinese);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }
}
