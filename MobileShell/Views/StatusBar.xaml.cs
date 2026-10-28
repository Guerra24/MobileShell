using MobileShell.Extensions;
using MobileShell.Services;
using MobileShell.ViewModels;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using XamlHostingKit;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell.Views;

public sealed partial class StatusBar : Grid
{
    public static readonly int AppBarHeight = 36;
    private static readonly uint AppBarEdge = ABE_TOP;

    private XamlWindow window;

    private SUBCLASSPROC windowSubclassProc = null!;
    private uint appBarMessage;

    private StatusBarViewModel Data;

    public StatusBar()
    {
        this.InitializeComponent();
        window = XamlWindow.Current;

        unsafe
        {
            windowSubclassProc = new SUBCLASSPROC(WindowSubclassProc);
            SetWindowSubclass((HWND)window.WindowHandle.Value, (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, nuint, nuint, LRESULT>)Marshal.GetFunctionPointerForDelegate(windowSubclassProc), 0, 0);
        }

        appBarMessage = window.AddAppBar();

        window.PositionAppBar(AppBarHeight, AppBarEdge);

        Data = Service.StatusBarViewModel;

        Data.OneHandedModeChanged += OneHandedModeChanged;

        window.CoreWindow.DispatcherQueue.ShutdownStarting += (s, e) => Dispose();
    }

    private void StatusBar_Loaded(object sender, RoutedEventArgs e)
    {
        var di = DisplayInformation.GetForCurrentView();
        di.DpiChanged += (s, e) =>
        {
            var monitor = window.MonitorRect();
            window.PositionAppBar(Data.OneHanded ? (float)monitor.Height / 2f : AppBarHeight, AppBarEdge);
        };
        di.OrientationChanged += (s, e) =>
        {
            var monitor = window.MonitorRect();
            window.PositionAppBar(Data.OneHanded ? (float)monitor.Height / 2f : AppBarHeight, AppBarEdge);
        };
    }

    private void Dispose()
    {
        window.RemoveAppBar();

        Data.OneHandedModeChanged -= OneHandedModeChanged;

        Data.Dispose();
    }

    private LRESULT WindowSubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == appBarMessage)
        {
            switch (wParam)
            {
                case ABN_POSCHANGED:
                    var monitor = window.MonitorRect();
                    window.PositionAppBar(Data.OneHanded ? (float)monitor.Height / 2f : AppBarHeight, AppBarEdge);
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

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void OneHandedGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Data.SetOneHanded(false);
    }

    private void OneHandedModeChanged()
    {
        var monitor = window.MonitorRect();
        window.PositionAppBar(Data.OneHanded ? (float)monitor.Height / 2f : AppBarHeight, AppBarEdge);
    }

}
