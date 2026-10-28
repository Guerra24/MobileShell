using MobileShell.Services;
using MobileShell.Views;
using TerraFX.Interop.Windows;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Xaml;
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

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

        var window = XamlWindow.Current;

        SetWindowPos((HWND)window.WindowHandle.Value, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP.SWP_NOMOVE | SWP.SWP_NOSIZE);

        window.ExtendedStyles = WindowExtendedStyles.Topmost | WindowExtendedStyles.NoRedirectionBitmap;
        window.Styles = WindowStyles.Popup;
        window.MinHeight = 0;
        window.MinWidth = 0;
        window.Content = new NavBar();

        window.Show();

        Task.Delay(500).ContinueWith((t) =>
        {
            XamlApplication.CreateNewWindow(new WindowCreationOptions
            {
                ExtendedStyles = WindowExtendedStyles.Topmost | WindowExtendedStyles.NoActivate | WindowExtendedStyles.NoRedirectionBitmap,
                Styles = WindowStyles.Popup,
            }, (p) =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

                var window = XamlWindow.Current;

                window.MinHeight = 0;
                window.MinWidth = 0;
                window.Content = new StatusBar();

                window.Show();
            });
        });

        XamlApplication.CreateNewWindow(new WindowCreationOptions
        {
            ExtendedStyles = WindowExtendedStyles.Topmost | WindowExtendedStyles.NoRedirectionBitmap,
            Styles = WindowStyles.Popup,
        }, (p) =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

            var window = XamlWindow.Current;

            window.MinHeight = 0;
            window.MinWidth = 0;
            window.Content = new TaskView();
        });
    }
}
