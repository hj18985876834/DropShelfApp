using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace DropShelf.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showShelfItem;
    private readonly Forms.ToolStripMenuItem _hideShelfItem;
    private readonly Forms.ToolStripMenuItem _settingsItem;
    private readonly Forms.ToolStripMenuItem _quitItem;
    private readonly Icon? _appIcon;
    private bool _disposed;

    public TrayIconService(
        Action showShelf,
        Action hideShelf,
        Action openSettings,
        Action quit,
        LocalizationService? localizationService = null)
    {
        ArgumentNullException.ThrowIfNull(showShelf);
        ArgumentNullException.ThrowIfNull(hideShelf);
        ArgumentNullException.ThrowIfNull(openSettings);
        ArgumentNullException.ThrowIfNull(quit);

        var texts = (localizationService ?? new LocalizationService()).Text;
        _showShelfItem = new Forms.ToolStripMenuItem(texts.TrayShowShelf, null, (_, _) => showShelf());
        _hideShelfItem = new Forms.ToolStripMenuItem(texts.TrayHideShelf, null, (_, _) => hideShelf());
        _settingsItem = new Forms.ToolStripMenuItem(texts.TraySettings, null, (_, _) => openSettings());
        _quitItem = new Forms.ToolStripMenuItem(texts.TrayQuit, null, (_, _) => quit());

        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add(_showShelfItem);
        _menu.Items.Add(_hideShelfItem);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(_settingsItem);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(_quitItem);

        _appIcon = LoadAppIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = _appIcon ?? SystemIcons.Application,
            Text = AppBranding.DefaultDisplayName,
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => showShelf();
        if (localizationService is not null)
        {
            localizationService.LanguageChanged += (_, _) => ApplyText(localizationService.Text);
        }

        SetShelfVisible(false);
    }

    public void SetShelfVisible(bool isVisible)
    {
        _showShelfItem.Enabled = !isVisible;
        _hideShelfItem.Enabled = isVisible;
    }

    public void ShowInfo(string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private void ApplyText(AppText texts)
    {
        _showShelfItem.Text = texts.TrayShowShelf;
        _hideShelfItem.Text = texts.TrayHideShelf;
        _settingsItem.Text = texts.TraySettings;
        _quitItem.Text = texts.TrayQuit;
    }

    private static Icon? LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "DropShelf.ico");
        return File.Exists(iconPath)
            ? new Icon(iconPath)
            : null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip = null;
        _notifyIcon.Dispose();
        _appIcon?.Dispose();
        _menu.Dispose();
        _disposed = true;
    }
}
