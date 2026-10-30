using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MobileShell.Models;
using MobileShell.Services;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.UI;
using Windows.UI.Core;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell.ViewModels;

public partial class TaskViewViewModel : ObservableObject
{
    private readonly CoreDispatcher Dispatcher;

    public ObservableCollection<TaskViewAppEntry> AppList = new();
    public event Func<Task> ShowTaskView = null!;
    public event Func<TaskViewAppEntry?, Task> HideTaskView = null!;
    public bool IsVisible { get; private set; }

    public TaskViewViewModel(CoreDispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }

    public async void Show()
    {
        if (IsVisible)
            return;
        IsVisible = true;

        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        var apps = FindWindows();

        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            AppList.Clear();
            apps.ForEach(a => AppList.Add(a));

            await ShowTaskView.Invoke();
        });
    }

    public async void Hide(TaskViewAppEntry? app = null)
    {
        if (!IsVisible)
            return;
        IsVisible = false;
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            await HideTaskView.Invoke(app ?? AppList.FirstOrDefault(app => Service.Monitor.Value == MonitorFromWindow(app.Handle, MONITOR.MONITOR_DEFAULTTONULL) && !IsIconic(app.Handle)));

            AppList.Clear();
        });
    }

    [RelayCommand]
    private unsafe void RestoreWindow(TaskViewAppEntry app)
    {
        var current = MonitorFromWindow(app.Handle, MONITOR.MONITOR_DEFAULTTOPRIMARY);
        var monitor = Service.Monitor.Value;

        if (current != monitor)
        {
            MONITORINFO mi = new()
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };

            GetMonitorInfoW(monitor, &mi);

            var rc = mi.rcWork;

            if (IsIconic(app.Handle))
                ShowWindow(app.Handle, SW.SW_RESTORE);

            if (IsZoomed(app.Handle))
                ShowWindow(app.Handle, SW.SW_RESTORE);

            SetWindowPos(app.Handle, HWND.HWND_TOP, rc.left, rc.top, 0, 0, SWP.SWP_NOSIZE | SWP.SWP_SHOWWINDOW);
        }

        ShowWindow(app.Handle, SW.SW_MAXIMIZE);

        SetForegroundWindow(app.Handle);

        Hide(app);
    }

    private static unsafe List<TaskViewAppEntry> FindWindows()
    {
        var results = new List<TaskViewAppEntry>();

        var gcHandle = GCHandle.Alloc(results);
        try
        {
            EnumWindows(&EnumWindowsCallback, (LPARAM)(IntPtr)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }

        return results;
    }

    [UnmanagedCallersOnly]
    private static unsafe BOOL EnumWindowsCallback(HWND hwnd, LPARAM lParam)
    {
        var gcHandle = GCHandle.FromIntPtr(lParam);
        var list = (List<TaskViewAppEntry>)gcHandle.Target!;

        if (IsValidWindow(hwnd))
        {
            var length = GetWindowTextLengthW(hwnd);
            var title = string.Empty;

            if (length > 0)
            {
                var buffer = stackalloc char[length + 1];
                GetWindowTextW(hwnd, buffer, length + 1);
                title = new string(buffer);
            }

            list.Add(new TaskViewAppEntry
            {
                Handle = hwnd,
                Title = title,
                WindowId = new WindowId(hwnd)
            });
        }

        return TRUE;
    }

    private static unsafe bool IsValidWindow(HWND hwnd)
    {
        if (!IsWindowVisible(hwnd))
            return false;

        if (GetShellWindow() == hwnd)
            return false;

        var style = GetWindowLongPtrW(hwnd, GWL.GWL_STYLE);
        var exStyle = GetWindowLongPtrW(hwnd, GWL.GWL_EXSTYLE);

        if ((style & WS.WS_POPUP) != 0 && (style & WS.WS_THICKFRAME) == 0 && (style & WS.WS_CAPTION) == 0)
            return false;

        if ((exStyle & WS.WS_EX_TOOLWINDOW) != 0 && (exStyle & WS.WS_EX_APPWINDOW) == 0)
            return false;

        var root = GetAncestor(hwnd, GA.GA_ROOTOWNER);
        HWND popup;
        while ((popup = GetLastActivePopup(root)) != popup)
        {
            if (IsWindowVisible(popup))
                break;
            root = popup;
        }
        if (root != hwnd)
            return false;

        var cloaked = 0;
        var hr = DwmGetWindowAttribute(hwnd, (uint)DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(int));
        if (SUCCEEDED(hr) && cloaked != 0)
            return false;

        return true;
    }

}
