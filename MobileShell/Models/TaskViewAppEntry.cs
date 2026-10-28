using TerraFX.Interop.Windows;
using Windows.UI;

namespace MobileShell.Models;

public class TaskViewAppEntry
{
    public string Title { get; set; } = null!;
    public HWND Handle { get; set; }
    public WindowId WindowId { get; set; }

    public override bool Equals(object? obj) => obj is TaskViewAppEntry entry && Handle == entry.Handle;

    public override int GetHashCode() => Handle.GetHashCode();
}
