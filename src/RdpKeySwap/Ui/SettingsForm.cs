using RdpKeySwap.Session;
using RdpKeySwap.Settings;

namespace RdpKeySwap.Ui;

internal sealed class SettingsForm : Form
{
    private readonly AppController _controller;
    private readonly ListBox _nameList = new();
    private readonly TextBox _nameInput = new();
    private readonly CheckBox _autoStart = new();
    private readonly Label _sessionValue = new();
    private readonly Label _clientValue = new();
    private readonly Label _applyValue = new();
    private readonly Label _reasonValue = new();
    private bool _syncing;

    public SettingsForm(AppController controller)
    {
        _controller = controller;

        BuildLayout();
        LoadState();

        _controller.StatusChanged += OnStatusChanged;
        FormClosed += (_, _) => _controller.StatusChanged -= OnStatusChanged;
    }

    internal static string ReasonText(InactiveReason reason) => reason switch
    {
        InactiveReason.SessionUnknown => "세션 종류를 확인할 수 없습니다.",
        InactiveReason.LocalSession => "원격 데스크톱 세션이 아닙니다.",
        InactiveReason.ClientNameUnavailable => "클라이언트 이름을 확인할 수 없습니다.",
        InactiveReason.NoRegisteredNames => "등록된 클라이언트 이름이 없습니다.",
        InactiveReason.ClientNotRegistered => "등록된 이름과 일치하지 않습니다.",
        _ => "조건이 일치합니다.",
    };

    private void BuildLayout()
    {
        Text = "RDP 키 스왑";
        ClientSize = new Size(500, 540);
        MinimumSize = new Size(440, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = AppIcon.Window;
        ShowIcon = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(14),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildStatusGroup(), 0, 0);
        root.Controls.Add(BuildNamesGroup(), 0, 1);
        root.Controls.Add(BuildAutoStartCheckBox(), 0, 2);
        root.Controls.Add(BuildRulesLabel(), 0, 3);
        root.Controls.Add(BuildFooter(), 0, 4);

        Controls.Add(root);
    }

    private GroupBox BuildStatusGroup()
    {
        var group = new GroupBox
        {
            Text = "상태",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10),
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddStatusRow(grid, 0, "세션", _sessionValue);
        AddStatusRow(grid, 1, "클라이언트", _clientValue);
        AddStatusRow(grid, 2, "적용", _applyValue);
        AddStatusRow(grid, 3, "사유", _reasonValue);

        group.Controls.Add(grid);
        return group;
    }

    private static void AddStatusRow(TableLayoutPanel grid, int row, string caption, Label value)
    {
        grid.RowCount = row + 1;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var captionLabel = new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 0, 12, 4),
        };

        value.AutoSize = true;
        value.Margin = new Padding(0, 0, 0, 4);

        grid.Controls.Add(captionLabel, 0, row);
        grid.Controls.Add(value, 1, row);
    }

    private GroupBox BuildNamesGroup()
    {
        var group = new GroupBox
        {
            Text = "Mac 클라이언트 이름",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 8),
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _nameList.Dock = DockStyle.Fill;
        _nameList.IntegralHeight = false;
        _nameList.SelectionMode = SelectionMode.One;
        _nameList.Margin = new Padding(0, 0, 0, 6);

        var inputRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
        };
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _nameInput.Dock = DockStyle.Fill;
        _nameInput.Margin = new Padding(0, 0, 6, 0);
        _nameInput.PlaceholderText = "예: example-mac";
        _nameInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            AddName();
        };

        var addButton = new Button { Text = "추가", AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        addButton.Click += (_, _) => AddName();

        var removeButton = new Button { Text = "삭제", AutoSize = true, Margin = new Padding(0) };
        removeButton.Click += (_, _) => RemoveName();

        inputRow.Controls.Add(_nameInput, 0, 0);
        inputRow.Controls.Add(addButton, 1, 0);
        inputRow.Controls.Add(removeButton, 2, 0);

        var hint = new Label
        {
            Text = "등록한 이름과 일치하는 RDP 접속에서만 키를 변환합니다.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 6, 0, 0),
        };

        grid.Controls.Add(_nameList, 0, 0);
        grid.Controls.Add(inputRow, 0, 1);
        grid.Controls.Add(hint, 0, 2);

        group.Controls.Add(grid);
        return group;
    }

    private CheckBox BuildAutoStartCheckBox()
    {
        _autoStart.Text = "Windows 로그인 시 자동 실행";
        _autoStart.AutoSize = true;
        _autoStart.Margin = new Padding(0, 0, 0, 6);
        _autoStart.CheckedChanged += OnAutoStartChanged;
        return _autoStart;
    }

    private static Label BuildRulesLabel() => new()
    {
        Text = "적용 규칙: Command → Alt, Option → Windows 키, Control → Ctrl",
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(0, 0, 0, 8),
    };

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var note = new Label
        {
            Text = "창을 닫아도 트레이에서 계속 실행됩니다.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
        };

        var closeButton = new Button
        {
            Text = "닫기",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Margin = new Padding(0),
        };
        closeButton.Click += (_, _) => Close();

        CancelButton = closeButton;

        footer.Controls.Add(note, 0, 0);
        footer.Controls.Add(closeButton, 1, 0);
        return footer;
    }

    private void LoadState()
    {
        ReloadNames();

        _syncing = true;
        _autoStart.Checked = AutoStart.IsEnabled();
        _syncing = false;

        UpdateStatus();
    }

    private void ReloadNames()
    {
        _nameList.BeginUpdate();
        _nameList.Items.Clear();
        foreach (var name in _controller.Settings.ClientNames)
        {
            _nameList.Items.Add(name);
        }

        _nameList.EndUpdate();
    }

    private void AddName()
    {
        var candidate = _nameInput.Text;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            _nameInput.Focus();
            return;
        }

        ApplyNames(_nameList.Items.Cast<string>().Append(candidate));
        _nameInput.Clear();
        _nameInput.Focus();
    }

    private void RemoveName()
    {
        var index = _nameList.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        var names = _nameList.Items.Cast<string>().ToList();
        names.RemoveAt(index);
        ApplyNames(names);
    }

    private void ApplyNames(IEnumerable<string> names)
    {
        _controller.UpdateClientNames(names);
        ReloadNames();
        UpdateStatus();
    }

    private void OnAutoStartChanged(object? sender, EventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        try
        {
            AutoStart.SetEnabled(_autoStart.Checked);
        }
        catch (Exception ex)
        {
            _syncing = true;
            _autoStart.Checked = !_autoStart.Checked;
            _syncing = false;

            MessageBox.Show(
                this,
                "자동 실행 설정을 저장하지 못했습니다.\n\n" + ex.Message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateStatus));
            return;
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var status = _controller.Status;

        _sessionValue.Text = status.Kind switch
        {
            SessionKind.Remote => $"원격 데스크톱 (세션 {status.SessionId})",
            SessionKind.Console => $"로컬 콘솔 (세션 {status.SessionId})",
            _ => $"확인할 수 없음 (세션 {status.SessionId})",
        };

        _clientValue.Text = status.ClientName ?? "확인할 수 없음";
        _applyValue.Text = status.IsActive ? "켜짐" : "꺼짐";
        _applyValue.ForeColor = status.IsActive ? Color.FromArgb(21, 128, 61) : SystemColors.ControlText;
        _reasonValue.Text = ReasonText(status.Reason);
    }
}
