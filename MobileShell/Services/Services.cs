using Microsoft.Extensions.DependencyInjection;
using MobileShell.ViewModels;
using MobileShell.ViewModels.Items;
using XamlHostingKit;

namespace MobileShell.Services;

public class Service
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void BuildServices(Action<ServiceCollection>? services = null)
    {
        var collection = new ServiceCollection();

        collection.AddTransient((s) => XamlWindow.Current.Dispatcher);

        collection.AddSingleton<StatusBarViewModel>();
        collection.AddSingleton<TaskViewViewModel>();
        collection.AddTransient<TaskViewAppViewModel>();

        services?.Invoke(collection);
        Services = collection.BuildServiceProvider();
    }

    public static StatusBarViewModel StatusBarViewModel => Services.GetRequiredService<StatusBarViewModel>();
    public static TaskViewViewModel TaskViewViewModel => Services.GetRequiredService<TaskViewViewModel>();

}
