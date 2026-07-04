using System.Drawing;
using Forms = System.Windows.Forms;

namespace DropShelf.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showShelfItem;
    private readonly Forms.ToolStripMenuItem _hideShelfItem;
    private bool _disposed;

    public TrayIconService(Action showShelf, Action hideShelf, Action openSettings, Action quit)
    {
        ArgumentNullException.ThrowIfNull(showShelf);
        ArgumentNullException.ThrowIfNull(hideShelf);
        ArgumentNullException.ThrowIfNull(openSettings);
        ArgumentNullException.ThrowIfNull(quit);

        _showShelfItem = new Forms.ToolStripMenuItem("Show Shelf", null, (_, _) => showShelf());
        _hideShelfItem = new Forms.ToolStripMenuItem("Hide Shelf", null, (_, _) => hideShelf());

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_showShelfItem);
        menu.Items.Add(_hideShelfItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Settings", null, (_, _) => openSettings()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Quit", null, (_, _) => quit()));

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = SystemIcons.Application,
            Text = "DropShelf",
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => showShelf();
        SetShelfVisible(false);
    }

    public void SetShelfVisible(bool isVisible)
    {
        _showShelfItem.Enabled = !isVisible;
        _hideShelfItem.Enabled = isVisible;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;
    }
}
