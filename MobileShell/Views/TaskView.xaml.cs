using CommunityToolkit.WinUI;
using MobileShell.Extensions;
using MobileShell.Models;
using MobileShell.Services;
using MobileShell.ViewModels;
using System.Numerics;
using TerraFX.Interop.Windows;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using WindowsUdk.UI.Shell;
using XamlHostingKit;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell.Views;

public sealed partial class TaskView : Grid
{

    private XamlWindow window;

    private WindowThumbnail? desktop;

    private TaskViewViewModel Data;

    public TaskView()
    {
        this.InitializeComponent();
        window = XamlWindow.Current;

        Data = Service.TaskViewViewModel;

        Data.ShowTaskView += ShowTaskView;
        Data.HideTaskView += HideTaskView;

        Canvas.SetTop(Desktop, -StatusBar.AppBarHeight);

        window.CoreWindow.DispatcherQueue.ShutdownStarting += (sender, args) => Dispose();
    }

    private void Dispose()
    {
        desktop?.Dispose();
    }

    private async Task ShowTaskView()
    {
        var area = window.MonitorRect();
        var work = window.MonitorWorkRect();

        window.Move((float)work.X, (float)work.Y);
        window.Resize((float)work.Width, (float)work.Height);

        var visual = ElementCompositionPreview.GetElementVisual(Desktop);

        desktop = WindowThumbnailGenerator.GetDesktopBackgroundThumbnail(window.WindowHandle, visual.Compositor, area.Width, area.Height);

        var virtualLeft = GetSystemMetrics(SM.SM_XVIRTUALSCREEN) / XamlRoot.RasterizationScale;
        var virtualTop = GetSystemMetrics(SM.SM_YVIRTUALSCREEN) / XamlRoot.RasterizationScale;

        desktop.Visual.Offset = new Vector3((float)virtualLeft, (float)virtualTop, 0);

        ElementCompositionPreview.SetElementChildVisual(Desktop, desktop.Visual);

        var app = Data.AppList.FirstOrDefault();

        if (app != null)
        {
            var scaledSource = new Size(app.ThumbnailFactory.SourceSize.Width / XamlRoot.RasterizationScale, app.ThumbnailFactory.SourceSize.Height / XamlRoot.RasterizationScale);

            using var thumbnail = app.ThumbnailFactory.CreateVisual(window.Compositor, scaledSource);

            ElementCompositionPreview.SetElementChildVisual(Foreground, thumbnail.Visual);

            await Task.Delay(5);

            window.Show();

            Apps.UpdateLayout();

            var container = Apps.ContainerFromItem(app);
            var targetElement = container.FindDescendant("Preview")!;

            var sourceTransform = Foreground.TransformToVisual(this);
            var targetTransform = targetElement.TransformToVisual(this);

            var sourcePoint = sourceTransform.TransformPoint(new Point(0, 0));
            var targetPoint = targetTransform.TransformPoint(new Point(0, 0));

            float scaleX = (float)(targetElement.ActualWidth / Foreground.ActualWidth);
            float scaleY = (float)(targetElement.ActualHeight / Foreground.ActualHeight);

            float deltaX = (float)(targetPoint.X - sourcePoint.X);
            float deltaY = (float)(targetPoint.Y - sourcePoint.Y);

            var easing = window.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.00f, 0.4f), new Vector2(0.2f, 1.0f));
            var duration = TimeSpan.FromMilliseconds(400);

            var scaleAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(scaleX, scaleY, 1.0f), easing);
            scaleAnim.Duration = duration;

            var offsetAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(1.0f, new Vector3(deltaX, deltaY, 0.0f), easing);
            offsetAnim.Duration = duration;

            var animationGroup = window.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            thumbnail.Visual.StartAnimation("Scale", scaleAnim);
            thumbnail.Visual.StartAnimation("Offset", offsetAnim);

            var tcs = new TaskCompletionSource();

            animationGroup.Completed += (s, e) => tcs.SetResult();
            animationGroup.End();

            await tcs.Task;

            ElementCompositionPreview.SetElementChildVisual(Foreground, null);
        }
        else
        {
            window.Show();
        }
    }

    private async Task HideTaskView(TaskViewAppEntry? app)
    {
        if (app != null)
        {

            var scaledSource = new Size(app.ThumbnailFactory.SourceSize.Width / XamlRoot.RasterizationScale, app.ThumbnailFactory.SourceSize.Height / XamlRoot.RasterizationScale);

            using var thumbnail = app.ThumbnailFactory.CreateVisual(window.Compositor, scaledSource);

            var container = Apps.ContainerFromItem(app);
            var targetElement = container.FindDescendant("Preview")!;

            var sourceTransform = Foreground.TransformToVisual(this);
            var targetTransform = targetElement.TransformToVisual(this);

            var sourcePoint = sourceTransform.TransformPoint(new Point(0, 0));
            var targetPoint = targetTransform.TransformPoint(new Point(0, 0));

            float scaleX = (float)(targetElement.ActualWidth / Foreground.ActualWidth);
            float scaleY = (float)(targetElement.ActualHeight / Foreground.ActualHeight);

            float deltaX = (float)(targetPoint.X - sourcePoint.X);
            float deltaY = (float)(targetPoint.Y - sourcePoint.Y);

            thumbnail.Visual.Scale = new Vector3(scaleX, scaleY, 1.0f);
            thumbnail.Visual.Offset = new Vector3(deltaX, deltaY, 0.0f);

            ElementCompositionPreview.SetElementChildVisual(Foreground, thumbnail.Visual);

            await Task.Delay(5);

            var easing = window.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.00f, 0.4f), new Vector2(0.2f, 1.0f));
            var duration = TimeSpan.FromMilliseconds(400);

            var scaleAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f), easing);
            scaleAnim.Duration = duration;

            var offsetAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(1.0f, new Vector3(0.0f, 0.0f, 0.0f), easing);
            offsetAnim.Duration = duration;

            var animationGroup = window.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            thumbnail.Visual.StartAnimation("Scale", scaleAnim);
            thumbnail.Visual.StartAnimation("Offset", offsetAnim);

            var tcs = new TaskCompletionSource();

            animationGroup.Completed += (s, e) => tcs.SetResult();
            animationGroup.End();

            await tcs.Task;
        }

        window.Hide();
        ElementCompositionPreview.SetElementChildVisual(Foreground, null);
        ElementCompositionPreview.SetElementChildVisual(Desktop, null);
        desktop?.Dispose();
        desktop = null;
    }

}
