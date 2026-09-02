using GooseShared;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
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
    private readonly Timer _activityTimer;
    
    // Flag to track if user has interacted with the form
    public bool HasUserInteracted { get; private set; }
    
    // Win32 API for setting foreground window
    [DllImport("user32.dll")]
    private static extern IntPtr SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public GooseInputForm(Point screenPos, string prompt, GooseEntity goose, Action<string> onSubmit)
    {
        _onSubmit = onSubmit;
        HasUserInteracted = false;
        
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
        
        // Configurar para que el formulario no robe el foco del input
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);

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
        
        // Auto-close after 15 seconds
        _autoCloseTimer = new Timer { Interval = 15000, Enabled = true };
        _autoCloseTimer.Tick += (s, args) => Close();
        
        // Timer para resetear el auto-close cuando hay actividad
        _activityTimer = new Timer { Interval = 500, Enabled = true };
        _activityTimer.Tick += (s, args) => 
        {
            if (_input.Focused)
                _autoCloseTimer.Stop();
            else
                _autoCloseTimer.Start();
        };
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
        this.Activate();
        this.BringToFront();
        
        // Force this window to be foreground
        SetForegroundWindow(this.Handle);
        
        // Retrasar el enfoque para asegurar que el formulario esté completamente mostrado
        BeginInvoke((MethodInvoker)delegate {
            _input.Focus();
            _input.Select();
        });
    }
    
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
    }
    
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _input.Focus();
        _input.Select();
        HasUserInteracted = true;
    }
    
    // Also mark as interacted when any mouse down happens on the form
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        HasUserInteracted = true;
        _input.Focus();
        _input.Select();
        // Force focus back to this window
        SetForegroundWindow(this.Handle);
    }
    
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        // Try to regain focus
        BeginInvoke((MethodInvoker)delegate {
            SetForegroundWindow(this.Handle);
            _input.Focus();
            _input.Select();
        });
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
    }
}
