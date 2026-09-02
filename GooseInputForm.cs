using GooseShared;
using System;
using System.Drawing;
using System.Windows.Forms;

public class GooseInputForm : Form
{
    private GooseAIConfig _cfg = GooseAIConfig.Load();
    internal readonly TextBox _input;
    private readonly Button _ok;
    private readonly Button _cancel;
    private readonly Label _promptLabel;
    private readonly Action<string> _onSubmit;
    private readonly Timer _autoCloseTimer;

    public GooseInputForm(Point screenPos, string prompt, GooseEntity goose, Action<string> onSubmit)
    {
        _onSubmit = onSubmit;
        
        // Sin botones de maximizar/minimizar
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;
        ForeColor = Color.Black;
        Padding = new Padding(10);
        ShowIcon = false;
        
        Location = new Point(screenPos.X, screenPos.Y - 45);
        
        TopMost = true;
        ShowInTaskbar = false;
        Opacity = 0.95;

        _promptLabel = new Label
        {
            Text = prompt,
            AutoSize = true,
            Parent = this,
            Location = new Point(Padding.Left, Padding.Top),
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };

        _input = new TextBox
        {
            Width = 250,
            Height = 25,
            Parent = this,
            Location = new Point(Padding.Left, _promptLabel.Bottom + 5),
            Font = new Font("Arial", 10),
            BorderStyle = BorderStyle.FixedSingle
        };

        _ok = new Button
        {
            Text = _cfg.bubbleEnterText,
            Parent = this,
            Location = new Point(Padding.Left, _input.Bottom + 5),
            Font = new Font("Arial", 10),
            Size = new Size(80, 25)
        };
        _ok.Click += (s, e) => HandleSubmit();

        _cancel = new Button
        {
            Text = "Cancel",
            Parent = this,
            Location = new Point(_ok.Right + 10, _input.Bottom + 5),
            Font = new Font("Arial", 10),
            Size = new Size(80, 25)
        };
        _cancel.Click += (s, e) => Close();

        _input.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true; e.SuppressKeyPress = true;
                HandleSubmit();
            }
            else if (e.KeyCode == Keys.Escape)
            { e.Handled = true; e.SuppressKeyPress = true; Close(); }
        };

        Resize += (s, e) =>
        {
            int radius = 15;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
            path.AddArc(0, Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            Region = new Region(path);
        };

        Width = Padding.Horizontal + Math.Max(_promptLabel.PreferredWidth, _input.Width + _ok.Width + 20);
        Height = _ok.Bottom + Padding.Bottom;
        MinimumSize = new Size(300, 120);

        Paint += (s, e) =>
        {
            using (var pen = new Pen(Color.Black, 2))
            {
                int radius = 15;
                e.Graphics.DrawArc(pen, 0, 0, radius, radius, 180, 90);
                e.Graphics.DrawArc(pen, Width - radius - 1, 0, radius, radius, 270, 90);
                e.Graphics.DrawArc(pen, Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
                e.Graphics.DrawArc(pen, 0, Height - radius - 1, radius, radius, 90, 90);
                e.Graphics.DrawLine(pen, radius / 2, 0, Width - radius / 2 - 1, 0);
                e.Graphics.DrawLine(pen, Width - 1, radius / 2, Width - 1, Height - radius / 2 - 1);
                e.Graphics.DrawLine(pen, radius / 2, Height - 1, Width - radius / 2 - 1, Height - 1);
                e.Graphics.DrawLine(pen, 0, radius / 2, 0, Height - radius / 2 - 1);
            }
        };
        
        // Auto-close timer: 15 seconds
        _autoCloseTimer = new Timer { Interval = 15000, Enabled = true };
        _autoCloseTimer.Tick += (s, e) => Close();
        
        // Reset timer on any activity
        _input.KeyDown += (s, e) => { _autoCloseTimer.Stop(); _autoCloseTimer.Start(); };
        _ok.Click += (s, e) => _autoCloseTimer.Stop();
        _cancel.Click += (s, e) => _autoCloseTimer.Stop();
        this.MouseMove += (s, e) => { _autoCloseTimer.Stop(); _autoCloseTimer.Start(); };
        this.MouseDown += (s, e) => { _autoCloseTimer.Stop(); _autoCloseTimer.Start(); };
    }

    private void HandleSubmit()
    {
        string input = _input.Text;
        if (!string.IsNullOrWhiteSpace(input) && _onSubmit != null)
            _onSubmit(input);
        Close();
    }
    
    public void UpdatePosition(Point goosePosition)
    {
        Location = new Point(goosePosition.X, goosePosition.Y - 45);
    }
    
    public bool IsInputFocused { get { return _input.Focused; } }
    
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Force focus to input box
        this.Activate();
        _input.Focus();
    }
}
