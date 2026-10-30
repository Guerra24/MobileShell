using Microsoft.Extensions.DependencyInjection;
using MobileShell.ViewModels;
using MobileShell.ViewModels.Items;
using System.Collections.Concurrent;
using XamlHostingKit;

namespace MobileShell.Services;

public class Service
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static ThreadLocal<TerraFX.Interop.Windows.HMONITOR> Monitor { get; } = new();
    public static ConcurrentDictionary<TerraFX.Interop.Windows.HMONITOR, StatusBarViewModel> StatusBar { get; } = new();
    public static ConcurrentDictionary<TerraFX.Interop.Windows.HMONITOR, TaskViewViewModel> TaskView { get; } = new();

    public static void BuildServices(Action<ServiceCollection>? services = null)
    {
        var collection = new ServiceCollection();

        collection.AddTransient((s) => XamlWindow.Current.Dispatcher);
        collection.AddTransient((s) => XamlWindow.Current.DispatcherQueue);

        collection.AddTransient<StatusBarViewModel>();
        collection.AddTransient<TaskViewViewModel>();
        collection.AddTransient<TaskViewAppViewModel>();

        services?.Invoke(collection);
        Services = collection.BuildServiceProvider();
    }

    public static StatusBarViewModel StatusBarViewModel => Services.GetRequiredService<StatusBarViewModel>();
    public static TaskViewViewModel TaskViewViewModel => Services.GetRequiredService<TaskViewViewModel>();

}
