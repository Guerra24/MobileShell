using MobileShell.Extensions;
using MobileShell.Services;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using WindowsUdk.UI.Shell;
using XamlHostingKit;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell.Views;

public sealed partial class NavBar : Grid
{
    private static readonly int AppBarHeight = 72;
    private static readonly uint AppBarEdge = ABE_BOTTOM;

    private HMONITOR monitor;
    private XamlWindow window;

    private ShellViewCoordinator shellStart, shellSearch;

    private DispatcherTimer backHoldTimer, startHoldTimer;

    private SUBCLASSPROC windowSubclassProc = null!;
    private uint appBarMessage;

    public NavBar(HMONITOR monitor)
    {
        this.InitializeComponent();
        this.monitor = monitor;
        window = XamlWindow.Current;

        SetTaskbarState(ABS_AUTOHIDE);

        unsafe
        {
            windowSubclassProc = new SUBCLASSPROC(WindowSubclassProc);
            SetWindowSubclass((HWND)window.WindowHandle.Value, (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, nuint, nuint, LRESULT>)Marshal.GetFunctionPointerForDelegate(windowSubclassProc), 0, 0);
        }

        appBarMessage = window.AddAppBar();

        window.PositionAppBar(monitor, AppBarHeight, AppBarEdge);

        shellStart = new(ShellView.Start);
        shellSearch = new(ShellView.Search);

        backHoldTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        backHoldTimer.Tick += BackHoldTimer_Tick;
        startHoldTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        startHoldTimer.Tick += StartHoldTimer_Tick;

        Back.AddHandler(PointerPressedEvent, new PointerEventHandler(Back_StartTimer), true);
        Start.AddHandler(PointerPressedEvent, new PointerEventHandler(Start_StartTimer), true);

        window.CoreWindow.DispatcherQueue.ShutdownStarting += (sender, args) => Dispose();
    }

    private void NavBar_Loaded(object sender, RoutedEventArgs e)
    {
        var di = DisplayInformation.GetForCurrentView();
        di.DpiChanged += (s, e) => window.PositionAppBar(monitor, AppBarHeight, AppBarEdge);
        di.OrientationChanged += (s, e) => window.PositionAppBar(monitor, AppBarHeight, AppBarEdge);
    }

    private void Dispose()
    {
        window.RemoveAppBar();

        SetTaskbarState(ABS_ALWAYSONTOP);
    }

    private unsafe void SetTaskbarState(LPARAM state)
    {
        fixed (char* className = "Shell_TrayWnd")
        fixed (char* name = "")
        {
            var taskbarHwnd = FindWindowW(className, name);

            /*APPBARDATA abd = new()
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = taskbarHwnd,
                lParam = state
            };

            SHAppBarMessage(ABM.ABM_SETSTATE, &abd);*/

            if (state == ABS_AUTOHIDE)
                ShowWindow(taskbarHwnd, SW.SW_HIDE);
            else
                ShowWindow(taskbarHwnd, SW.SW_SHOW);
        }

        fixed (char* className = "Shell_SecondaryTrayWnd")
        fixed (char* name = "")
        {
            var taskbarHwnd = FindWindowW(className, name);

            /*APPBARDATA abd = new()
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = taskbarHwnd,
                lParam = state
            };

            SHAppBarMessage(ABM.ABM_SETSTATE, &abd);*/

            if (state == ABS_AUTOHIDE)
                ShowWindow(taskbarHwnd, SW.SW_HIDE);
            else
                ShowWindow(taskbarHwnd, SW.SW_SHOW);
        }
    }

    private LRESULT WindowSubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        switch (uMsg)
        {
            case WM.WM_MOUSEACTIVATE:
                if (Service.TaskView[monitor].IsVisible ||
                    shellStart.Visibility == ViewVisibility.Visible ||
                    shellSearch.Visibility == ViewVisibility.Visible)
                {
                    return MA.MA_NOACTIVATE;
                }
                break;
            default:
                if (uMsg == appBarMessage)
                {
                    switch (wParam)
                    {
                        case ABN_POSCHANGED:
                            window.PositionAppBar(monitor, AppBarHeight, AppBarEdge);
                            SetTaskbarState(ABS_AUTOHIDE);
                            return TRUE;
                        case ABN_FULLSCREENAPP:
                            if (lParam == 1)
                            {
                                SetWindowPos((HWND)window.WindowHandle.Value, HWND.HWND_BOTTOM, 0, 0, 0, 0, SWP.SWP_NOMOVE | SWP.SWP_NOSIZE);
                            }
                            else
                            {
                                SetWindowPos((HWND)window.WindowHandle.Value, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP.SWP_NOMOVE | SWP.SWP_NOSIZE);
                            }
                            break;
                    }
                }
                break;
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        XamlApplication.Close();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (Service.TaskView[monitor].IsVisible)
            Service.TaskView[monitor].Hide();
        else
        {
            unsafe
            {
                INPUT* inputs = stackalloc INPUT[4];

                inputs[0].type = INPUT.INPUT_KEYBOARD;
                inputs[0].ki.wVk = VK.VK_LWIN;

                inputs[1].type = INPUT.INPUT_KEYBOARD;
                inputs[1].ki.wVk = VK.VK_BACK;

                inputs[2].type = INPUT.INPUT_KEYBOARD;
                inputs[2].ki.wVk = VK.VK_BACK;
                inputs[2].ki.dwFlags = KEYEVENTF_KEYUP;

                inputs[3].type = INPUT.INPUT_KEYBOARD;
                inputs[3].ki.wVk = VK.VK_LWIN;
                inputs[3].ki.dwFlags = KEYEVENTF_KEYUP;

                SendInput(4, inputs, sizeof(INPUT));
            }
        }
    }

    private void BackHoldTimer_Tick(object? sender, object e)
    {
        backHoldTimer.Stop();
        Back.ReleasePointerCaptures();
        Service.TaskView[monitor].Show();
    }

    private void Back_StartTimer(object sender, PointerRoutedEventArgs e)
    {
        backHoldTimer.Start();
    }

    private void Back_StopTimer(object sender, PointerRoutedEventArgs e)
    {
        backHoldTimer.Stop();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (shellStart.Visibility == ViewVisibility.Hidden)
            await shellStart.TryShowAsync(new ShowShellViewOptions
            {
                InvokerWindowId = window.WindowHandle.Value,
                InputDeviceKind = InputDeviceKind.Mouse,
                TransferForegroundRights = true
            });
        else
            await shellStart.TryDismissAsync(new DismissShellViewOptions { InputDeviceKind = InputDeviceKind.Mouse });
    }

    private void StartHoldTimer_Tick(object? sender, object e)
    {
        startHoldTimer.Stop();
        Start.ReleasePointerCaptures();
        Service.StatusBar[monitor].SetOneHanded(!Service.StatusBar[monitor].OneHanded);
        Task.Delay(500).ContinueWith(t => window.PositionAppBar(monitor, AppBarHeight, AppBarEdge));
    }

    private void Start_StartTimer(object sender, PointerRoutedEventArgs e)
    {
        startHoldTimer.Start();
    }

    private void Start_StopTimer(object sender, PointerRoutedEventArgs e)
    {
        startHoldTimer.Stop();
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        if (shellSearch.Visibility == ViewVisibility.Hidden)
            await shellSearch.TryShowAsync(new ShowShellViewOptions
            {
                InvokerWindowId = window.WindowHandle.Value,
                InputDeviceKind = InputDeviceKind.Mouse,
                TransferForegroundRights = true
            });
        else
            await shellSearch.TryDismissAsync(new DismissShellViewOptions { InputDeviceKind = InputDeviceKind.Mouse });
    }

}
