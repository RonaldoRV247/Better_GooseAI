using System;
using System.Drawing;
using System.Windows.Forms;

public class GooseInputForm : Form
{
    private GooseAIConfig _cfg = GooseAIConfig.Load();

    private readonly TextBox _input;
    private readonly Button _ok;
    private readonly Label _promptLabel;

    private static GooseInputForm _instance;

    public string UserInput => _input.Text;

    private GooseInputForm(Point screenPos, string prompt)
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;
        ForeColor = Color.Black;
        Padding = new Padding(10);
        
        // Asegurar que el formulario tenga un tamaño inicial conocido
        int formWidth = 300;
        int formHeight = 120;
        
        // Usar el helper para posicionar el formulario de manera segura
        Point safePosition = MultiMonitorHelper.GetSafePopupPosition(
            screenPos, -100, -80, formWidth, formHeight);
        
        Location = safePosition;
        
        // Asegurar que la ventana siempre esté visible
        TopMost = true;
        ShowInTaskbar = false;
        
        // Mejorar visibilidad
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
            DialogResult = DialogResult.OK,
            Font = new Font("Arial", 10),
            Size = new Size(80, 25)
        };
        _ok.Click += (s, e) => Close();

        _input.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        // Estilo de bordes redondeados
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

        // Tamaño más generoso
        Width = Padding.Horizontal + Math.Max(_promptLabel.PreferredWidth, _input.Width);
        Height = _ok.Bottom + Padding.Bottom;
        
        // Asegurar tamaño mínimo
        MinimumSize = new Size(300, 120);
        
        // Añadir borde para mejor visibilidad
        Paint += (s, e) =>
        {
            using (var pen = new Pen(Color.Black, 2))
            {
                int radius = 15;
                e.Graphics.DrawArc(pen, 0, 0, radius, radius, 180, 90);
                e.Graphics.DrawArc(pen, Width - radius - 1, 0, radius, radius, 270, 90);
                e.Graphics.DrawArc(pen, Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
                e.Graphics.DrawArc(pen, 0, Height - radius - 1, radius, radius, 90, 90);
                
                // Líneas rectas entre los arcos
                e.Graphics.DrawLine(pen, radius / 2, 0, Width - radius / 2 - 1, 0);
                e.Graphics.DrawLine(pen, Width - 1, radius / 2, Width - 1, Height - radius / 2 - 1);
                e.Graphics.DrawLine(pen, radius / 2, Height - 1, Width - radius / 2 - 1, Height - 1);
                e.Graphics.DrawLine(pen, 0, radius / 2, 0, Height - radius / 2 - 1);
            }
        };
    }

    public static string ShowAt(Point screenPos, string prompt)
    {
        // Cerrar instancia existente si la hay
        if (_instance != null)
        {
            _instance.Close();
            _instance.Dispose();
            _instance = null;
        }

        // Asegurar que la posición esté dentro de los límites de alguna pantalla
        Point safePosition = EnsurePositionOnScreen(screenPos);
        
        _instance = new GooseInputForm(safePosition, prompt);

        _instance.Shown += (s, e) => _instance._input.Focus();

        string input = string.Empty;
        if (_instance.ShowDialog() == DialogResult.OK)
            input = _instance.UserInput;

        _instance.Dispose();
        _instance = null;

        return input;
    }
    
    private static Point EnsurePositionOnScreen(Point position)
    {
        // Usar el helper para asegurarnos de que la posición está en una pantalla válida
        Point safePosition = MultiMonitorHelper.EnsurePointOnScreen(position);
        
        // Luego ajustar para el tamaño del formulario
        return MultiMonitorHelper.GetSafePopupPosition(
            safePosition, -100, -80, 300, 120);
    }
}
