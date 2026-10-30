using MobileShell.Services;
using MobileShell.Views;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using XamlHostingKit;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell;

public partial class App : Application
{

    public App()
    {
        Service.BuildServices();
        this.InitializeComponent();
    }

    protected override unsafe void OnLaunched(LaunchActivatedEventArgs args)
    {
        EnumDisplayMonitors(HDC.NULL, null, &MonitorEnum, 0);

        var window = XamlWindow.Current;

        window.Content = new Grid();

        //Task.Delay(1000).ContinueWith(t => window.Close());

        //window.DispatcherQueue.TryEnqueue(() => window.Close());

        //Thread.Sleep(10000);
    }

    [UnmanagedCallersOnly]
    private static unsafe BOOL MonitorEnum(HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM dwData)
    {
        CreateShell(hMonitor);
        return TRUE;
    }

    private static void CreateShell(HMONITOR monitor)
    {
        XamlApplication.CreateNewWindow(new WindowCreationOptions
        {
            ExtendedStyles = WindowExtendedStyles.Topmost | WindowExtendedStyles.NoRedirectionBitmap,
            Styles = WindowStyles.Popup,
        }, (p) =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            Service.Monitor.Value = monitor;

            var window = XamlWindow.Current;

            window.MinHeight = 0;
            window.MinWidth = 0;
            window.Content = new TaskView(monitor);
        });

        XamlApplication.CreateNewWindow(new WindowCreationOptions
        {
            ExtendedStyles = WindowExtendedStyles.Topmost | WindowExtendedStyles.NoActivate | WindowExtendedStyles.NoRedirectionBitmap,
            Styles = WindowStyles.Popup,
        }, (p) =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            Service.Monitor.Value = monitor;

            var window = XamlWindow.Current;

            window.MinHeight = 0;
            window.MinWidth = 0;
            window.Content = new NavBar(monitor);

            window.Show();
        });

        XamlApplication.CreateNewWindow(new WindowCreationOptions
        {
            ExtendedStyles = WindowExtendedStyles.Topmost | WindowExtendedStyles.NoActivate | WindowExtendedStyles.NoRedirectionBitmap,
            Styles = WindowStyles.Popup,
        }, (p) =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            Service.Monitor.Value = monitor;

            var window = XamlWindow.Current;

            window.MinHeight = 0;
            window.MinWidth = 0;
            window.Content = new StatusBar(monitor);

            window.Show();
        });
    }
}
