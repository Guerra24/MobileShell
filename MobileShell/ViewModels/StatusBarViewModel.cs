using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Devices.Radios;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Windows.UI.Xaml;
using WindowsUdk.Foundation.Metadata;
using WindowsUdk.UI.Notifications;
using WindowsUdk.UI.Shell;
using WindowsUdk.UI.Shell.PowerUX;
using XamlHostingKit;

namespace MobileShell.ViewModels;

public partial class StatusBarViewModel : ObservableObject, IDisposable
{
    private readonly CoreDispatcher Dispatcher;

    [ObservableProperty]
    public partial NetworkIconInfo NetworkIconInfo { get; set; }
    [ObservableProperty]
    public partial BatteryIconData? BatteryIconData { get; set; }
    [ObservableProperty]
    public partial string Clock { get; set; }
    [ObservableProperty]
    public partial bool DoNotDisturb { get; set; }
    [ObservableProperty]
    public partial bool BluetoothEnabled { get; set; }
    public bool OneHanded { get; private set; }
    public event Action? OneHandedModeChanged;

    private BatteryIcon? batteryIcon;
    private DispatcherTimer clockTimer = new(), generalTimer = new();
    private readonly DateTimeFormatter clockFormatter = new("hour minute");
    private NotificationsSettings notificationsSettings;
    private Radio? bluetooth;
    private ShellViewCoordinator shellActionCenter, shellControlCenter;

    public StatusBarViewModel(CoreDispatcher dispatcher)
    {
        Dispatcher = dispatcher;

        UndockedDevKit.Wrapper.NetworkIcon.HookEvent(dispatcher = XamlWindow.Current.Dispatcher);

        NetworkIconInfo = NetworkIcon.GetPrimaryIconInfo();
        UndockedDevKit.Wrapper.NetworkIcon.PrimaryIconInfoChanged += NetworkIcon_PrimaryIconInfoChanged;

        if (ApiInformation.IsApiContractPresent("WindowsUdk.UI.Shell.PowerUX.BatteryIconContract", 1))
        {
            batteryIcon = BatteryIcon.GetLocalCompositeBatteryIcon();
            batteryIcon.IconDataChanged += BatteryIcon_IconDataChanged;
            BatteryIconData = batteryIcon.GetIconData();
        }

        clockTimer.Interval = TimeSpan.FromMilliseconds(500);
        clockTimer.Tick += ClockTimer_Tick;
        clockTimer.Start();
        Clock = clockFormatter.Format(DateTimeOffset.Now);

        notificationsSettings = NotificationsSettings.GetDefault();
        DoNotDisturb = notificationsSettings.DoNotDisturbEnabled;

        generalTimer.Interval = TimeSpan.FromSeconds(1);
        generalTimer.Tick += GeneralTimer_Tick;
        generalTimer.Start();

        shellActionCenter = new(ShellView.ActionCenter);
        shellControlCenter = new(ShellView.ControlCenter);

        InitRadios();
    }

    private async void InitRadios()
    {
        var res = await Radio.RequestAccessAsync();
        if (res != RadioAccessStatus.Allowed)
            return;

        var radios = await Radio.GetRadiosAsync();

        foreach (var radio in radios)
        {
            if (radio.Kind == RadioKind.Bluetooth)
            {
                bluetooth = radio;
                bluetooth.StateChanged += Bluetooth_StateChanged;
                Bluetooth_StateChanged(radio, null!);
                break;
            }
        }
    }

    private async void Bluetooth_StateChanged(Radio sender, object e)
    {
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BluetoothEnabled = sender.State == RadioState.On);
    }

    private void NetworkIcon_PrimaryIconInfoChanged(object? sender, PrimaryIconInfoChangedEventArgs e)
    {
        NetworkIconInfo = NetworkIcon.GetPrimaryIconInfo();
    }

    private async void BatteryIcon_IconDataChanged(BatteryIcon sender, object args)
    {
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            BatteryIconData = batteryIcon!.GetIconData();
        });
    }

    private void ClockTimer_Tick(object? sender, object e)
    {
        Clock = clockFormatter.Format(DateTimeOffset.Now);
    }

    private void GeneralTimer_Tick(object? sender, object e)
    {
        DoNotDisturb = notificationsSettings.DoNotDisturbEnabled;
    }

    public void SetOneHanded(bool state)
    {
        OneHanded = state;
        OneHandedModeChanged?.Invoke();
    }

    [RelayCommand]
    private async Task ActionCenter()
    {
        if (shellActionCenter.Visibility == ViewVisibility.Hidden)
            await shellActionCenter.TryShowAsync(new ShowShellViewOptions());
        else
            await shellActionCenter.TryDismissAsync(new DismissShellViewOptions());
    }

    [RelayCommand]
    private async Task ControlCenter()
    {
        if (shellControlCenter.Visibility == ViewVisibility.Hidden)
            await shellControlCenter.TryShowAsync(new ShowShellViewOptions());
        else
            await shellControlCenter.TryDismissAsync(new DismissShellViewOptions());
    }

    public void Dispose()
    {
        bluetooth?.StateChanged -= Bluetooth_StateChanged;

        generalTimer.Stop();
        clockTimer.Stop();

        batteryIcon?.IconDataChanged -= BatteryIcon_IconDataChanged;
        UndockedDevKit.Wrapper.NetworkIcon.PrimaryIconInfoChanged -= NetworkIcon_PrimaryIconInfoChanged; ;

        UndockedDevKit.Wrapper.NetworkIcon.UnhookEvent();
    }
}
