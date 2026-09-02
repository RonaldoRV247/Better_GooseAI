using GooseShared;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class ModMain : IMod
{
    private readonly TimeSpan _idleThreshold = TimeSpan.FromMinutes(2);
    private bool _aiTriggered = false, _bubbleAttached = false;

    // Form instance for position tracking
    private static GooseInputForm _inputForm = null;

    // Double-click fields
    private int _clickCount = 0;
    private DateTime _lastClickTime = DateTime.MinValue;
    private const int MaxClickIntervalMs = 500;
    private const float ClickRadius = 50f;

    [DllImport("User32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO { public uint cbSize, dwTime; }

    public void Init()
    {
        InjectionPoints.PostTickEvent += OnTick;
    }

    private void OnTick(GooseEntity goose)
    {
        // Attach speech-bubble renderer once
        if (!_bubbleAttached)
        {
            goose.render += SpeechBubble.Draw;
            _bubbleAttached = true;
        }

        // Update input form position if it's open and input doesn't have focus
        if (_inputForm != null && !_inputForm.IsInputFocused)
        {
            _inputForm.UpdatePosition(new Point((int)goose.position.x, (int)goose.position.y));
        }

        // Idle trigger
        CheckIdle(goose);

        // Double-click trigger
        CheckClicks(goose);
    }

    private void CheckIdle(GooseEntity goose)
    {
        if (!_aiTriggered && GetIdleTime() > _idleThreshold)
        {
            new TaskAIInteraction().RunTask(goose);
            _aiTriggered = true;
        }
        else if (GetIdleTime() < TimeSpan.FromSeconds(10))
        {
            _aiTriggered = false;
        }
    }

    private void CheckClicks(GooseEntity goose)
    {
        // Only on mouse-down edge
        const int VK_LBUTTON = 0x01;
        bool isDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        if (isDown && _lastClickTime != DateTime.MinValue && (DateTime.Now - _lastClickTime).TotalMilliseconds < 50)
            return;  // still held

        if (isDown)
        {
            var now = DateTime.Now;
            // reset if too slow
            if ((now - _lastClickTime).TotalMilliseconds > MaxClickIntervalMs)
                _clickCount = 0;

            // get cursor pos in screen coords
            var cursor = Cursor.Position;
            
            // If input form is open, ignore ALL clicks near the goose
            // This prevents the goose from being dragged when user clicks on form
            if (_inputForm != null)
            {
                // Calculate expanded form bounds (include some padding)
                Rectangle formBounds = new Rectangle(
                    _inputForm.Location.X - 50, 
                    _inputForm.Location.Y - 50,
                    _inputForm.Width + 100, 
                    _inputForm.Height + 100
                );
                
                if (formBounds.Contains(cursor))
                {
                    // Click is inside or very near the form - ignore it
                    _clickCount = 0;
                    _lastClickTime = now;
                    return;
                }
            }

            // game coords are also screen coords for Desktop Goose
            float dx = cursor.X - goose.position.x;
            float dy = cursor.Y - goose.position.y;
            if (dx * dx + dy * dy < ClickRadius * ClickRadius)
            {
                _clickCount++;
                if (_clickCount >= 2)
                {
                    // Only trigger if input form is not already open
                    if (_inputForm == null)
                    {
                        new TaskAIInteraction().RunTask(goose);
                    }
                    _clickCount = 0;
                }
            }
            else
            {
                _clickCount = 0;
            }
            _lastClickTime = now;
        }
    }

    private TimeSpan GetIdleTime()
    {
        var lastIn = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
        GetLastInputInfo(ref lastIn);
        uint idleMs = (uint)(Environment.TickCount - lastIn.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }
    
    public static void SetInputForm(GooseInputForm form)
    {
        if (_inputForm != null)
        {
            _inputForm.Close();
            _inputForm.Dispose();
        }
        _inputForm = form;
    }
    
    public static void ClearInputForm()
    {
        _inputForm = null;
    }
}
