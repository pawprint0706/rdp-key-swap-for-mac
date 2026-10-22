namespace RdpKeySwap.Ui;

/// <summary>Tray-resident host: owns the controller, the tray icon and the settings window.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int MaxTrayTextLength = 63;

    private readonly AppController _controller;
    private readonly NotifyIcon _tray;
    private SettingsForm? _settingsForm;
    private bool _exiting;

    public TrayApplicationContext(bool startInTray)
    {
        _controller = new AppController();

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("설정 열기", null, (_, _) => ShowSettings()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("종료", null, (_, _) => Exit()));

        _tray = new NotifyIcon
        {
            Icon = AppIcon.Tray,
            Text = "RDP 키 스왑",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => ShowSettings();

        _controller.StatusChanged += (_, _) => UpdateTrayText();
        UpdateTrayText();

        if (!startInTray)
        {
            ShowSettings();
        }
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false } existing)
        {
            existing.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_controller);
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void UpdateTrayText()
    {
        var status = _controller.Status;
        var text = status.IsActive
            ? $"RDP 키 스왑 — 켜짐 ({status.ClientName})"
            : $"RDP 키 스왑 — 꺼짐: {SettingsForm.ReasonText(status.Reason)}";

        _tray.Text = text.Length > MaxTrayTextLength ? text[..(MaxTrayTextLength - 1)] + "…" : text;
    }

    private void Exit()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;

        _settingsForm?.Close();
        _tray.Visible = false;
        _tray.Dispose();
        _controller.Dispose();
        AppIcon.Dispose();
        ExitThread();
    }
}
