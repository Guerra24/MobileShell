using TerraFX.Interop.Windows;
using WindowsUdk.UI.Shell;

namespace MobileShell.Models;

public class TaskViewAppEntry
{
    public string Title { get; set; } = null!;
    public HWND Handle { get; set; }
    public WindowThumbnailFactory ThumbnailFactory { get; set; } = null!;

    public override bool Equals(object? obj) => obj is TaskViewAppEntry entry && Handle == entry.Handle;

    public override int GetHashCode() => Handle.GetHashCode();
}
