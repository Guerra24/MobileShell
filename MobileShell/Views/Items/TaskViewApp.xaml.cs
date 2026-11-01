using Microsoft.Extensions.DependencyInjection;
using MobileShell.Models;
using MobileShell.Services;
using MobileShell.ViewModels.Items;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

namespace MobileShell.Views.Items;

public sealed partial class TaskViewApp : UserControl
{

    private TaskViewAppViewModel Data;

    private Visual rootVisual = null!;

    private bool manipulating;

    public TaskViewApp()
    {
        this.InitializeComponent();
        Data = Service.Services.GetRequiredService<TaskViewAppViewModel>();
    }

    private void TaskViewApp_Loaded(object sender, RoutedEventArgs e)
    {
        rootVisual = ElementCompositionPreview.GetElementVisual(Root);
    }

    private async void TaskViewApp_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        rootVisual?.Offset = new(0, 0, 0);
        rootVisual?.Opacity = 1.0f;
        Data.Load(args.NewValue as TaskViewAppEntry, Preview);
    }

    private void TaskViewApp_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        manipulating = true;
        e.Handled = true;
    }

    private void TaskViewApp_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var offset = Math.Max((float)e.Cumulative.Translation.Y, 0.0f);
        rootVisual.Offset = new(0, offset, 0);
        rootVisual.Opacity = float.Lerp(1.0f, 0.0f, offset / 300f);
        if (offset >= 300f && e.IsInertial)
            e.Complete();
        e.Handled = true;
    }

    private void TaskViewApp_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        if (e.Cumulative.Translation.Y >= 300f)
        {
            Data.CloseWindow();
            e.Handled = true;
        }
        else if (e.Cumulative.Translation.Y > 0)
        {
            var compositor = rootVisual.Compositor;

            var easing = compositor.CreateCubicBezierEasingFunction(new(0.00f, 0.4f), new(0.2f, 1.0f));
            var duration = TimeSpan.FromMilliseconds(500);

            var offsetAnim = compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(1.0f, new(0.0f, 0.0f, 0.0f), easing);
            offsetAnim.Duration = duration;

            var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, easing);
            opacityAnim.Duration = duration;

            var animationGroup = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            rootVisual.StartAnimation("Offset", offsetAnim);
            rootVisual.StartAnimation("Opacity", opacityAnim);
            animationGroup.Completed += (s, e) => manipulating = false;
            animationGroup.End();
            e.Handled = true;
        }
    }

    private void TaskViewApp_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = manipulating;
    }

    private void TaskViewApp_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = manipulating;
    }
}
