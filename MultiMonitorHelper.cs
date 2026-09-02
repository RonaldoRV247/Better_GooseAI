using System.Drawing;
using System.Windows.Forms;

/// <summary>
/// Helper class for multi-monitor support in GooseAI mod
/// </summary>
public static class MultiMonitorHelper
{
    /// <summary>
    /// Get the screen that contains the specified point
    /// </summary>
    public static Screen GetScreenForPoint(Point point)
    {
        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.Bounds.Contains(point))
                return screen;
        }
        return Screen.PrimaryScreen; // Fallback to primary screen
    }
    
    /// <summary>
    /// Get the screen that contains the goose position
    /// </summary>
    public static Screen GetScreenForGoose(GooseShared.GooseEntity goose)
    {
        Point goosePos = new Point((int)goose.position.x, (int)goose.position.y);
        return GetScreenForPoint(goosePos);
    }
    
    /// <summary>
    /// Ensure the point is within the bounds of a valid screen
    /// </summary>
    public static Point EnsurePointOnScreen(Point point)
    {
        Screen screen = GetScreenForPoint(point);
        Rectangle bounds = screen.Bounds;
        
        // Clamp the point to be within the screen bounds
        int safeX = Clamp(point.X, bounds.Left, bounds.Right - 1);
        int safeY = Clamp(point.Y, bounds.Top, bounds.Bottom - 1);
        
        return new Point(safeX, safeY);
    }
    
    /// <summary>
    /// Get a safe position for a popup relative to the goose, ensuring it stays on screen
    /// </summary>
    public static Point GetSafePopupPosition(Point goosePosition, int offsetX, int offsetY, int popupWidth, int popupHeight)
    {
        Point popupPos = new Point(goosePosition.X + offsetX, goosePosition.Y + offsetY);
        Screen screen = GetScreenForPoint(goosePosition);
        Rectangle screenBounds = screen.Bounds;
        
        // If popup would go off-screen horizontally, adjust offset
        if (popupPos.X < screenBounds.Left)
            popupPos.X = screenBounds.Left + 10;
        else if (popupPos.X + popupWidth > screenBounds.Right)
            popupPos.X = screenBounds.Right - popupWidth - 10;
            
        // If popup would go off-screen vertically, adjust offset
        if (popupPos.Y < screenBounds.Top)
            popupPos.Y = screenBounds.Top + 10;
        else if (popupPos.Y + popupHeight > screenBounds.Bottom)
            popupPos.Y = screenBounds.Bottom - popupHeight - 10;
            
        return popupPos;
    }
    
    /// <summary>
    /// Check if a point is within any screen bounds
    /// </summary>
    public static bool IsPointOnAnyScreen(Point point)
    {
        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.Bounds.Contains(point))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get the current screen where the cursor is located
    /// </summary>
    public static Screen GetCurrentCursorScreen()
    {
        Point cursorPos = Cursor.Position;
        return GetScreenForPoint(cursorPos);
    }
    
    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}