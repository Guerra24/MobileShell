using Microsoft.Extensions.DependencyInjection;
using MobileShell.Models;
using MobileShell.Services;
using MobileShell.ViewModels.Items;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MobileShell.Views.Items;

public sealed partial class TaskViewApp : UserControl
{

    private TaskViewAppViewModel Data;

    public TaskViewApp()
    {
        this.InitializeComponent();
        Data = Service.Services.GetRequiredService<TaskViewAppViewModel>();
    }

    private async void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        Data.Load(args.NewValue as TaskViewAppEntry, Preview);
    }

}
