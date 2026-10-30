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

    private HMONITOR monitor;
    private XamlWindow window;

    private WindowThumbnail? desktop;

    public TaskViewViewModel Data { get; private set; }

    public TaskView(HMONITOR monitor)
    {
        this.InitializeComponent();
        this.monitor = monitor;
        window = XamlWindow.Current;

        Service.TaskView[monitor] = Data = Service.TaskViewViewModel;

        Data.ShowTaskView += ShowTaskView;
        Data.HideTaskView += HideTaskView;

        window.CoreWindow.DispatcherQueue.ShutdownStarting += (sender, args) => Dispose();
    }

    private void Dispose()
    {
        desktop?.Dispose();
    }

    private async Task ShowTaskView()
    {
        var area = monitor.MonitorRect(1.0f);
        var work = monitor.MonitorWorkRect();

        window.Move((float)work.X, (float)work.Y);
        window.Resize((float)work.Width, (float)work.Height);

        desktop = WindowThumbnailGenerator.GetDesktopBackgroundThumbnail(window.WindowHandle, window.Compositor, area.Width, area.Height);

        var virtualLeft = GetSystemMetrics(SM.SM_XVIRTUALSCREEN) / XamlRoot.RasterizationScale;
        var virtualTop = GetSystemMetrics(SM.SM_YVIRTUALSCREEN) / XamlRoot.RasterizationScale;
        var scale = 1.0f / (float)XamlRoot.RasterizationScale;

        desktop.Visual.Offset = new((float)(virtualLeft - work.Left), (float)(virtualTop - work.Top), 0);
        desktop.Visual.Scale = new(scale, scale, 1.0f);

        ElementCompositionPreview.SetElementChildVisual(Desktop, desktop.Visual);

        var app = Data.AppList.FirstOrDefault(app => monitor == MonitorFromWindow(app.Handle, MONITOR.MONITOR_DEFAULTTONULL) && !IsIconic(app.Handle));

        if (app != null)
        {
            var factory = new UndockedDevKit.Wrapper.WindowThumbnailFactory(app.WindowId);

            var scaledSource = new Size(factory.SourceSize.Width / XamlRoot.RasterizationScale, factory.SourceSize.Height / XamlRoot.RasterizationScale);

            using var thumbnail = factory.CreateVisual(window.Compositor, scaledSource);

            var relativeX = factory.SourceBounds.X / XamlRoot.RasterizationScale - window.Bounds.X;
            var relativeY = factory.SourceBounds.Y / XamlRoot.RasterizationScale - window.Bounds.Y;

            thumbnail.Visual.Offset = new((float)relativeX, (float)relativeY, 0.0f);

            ElementCompositionPreview.SetElementChildVisual(Foreground, thumbnail.Visual);

            await Task.Delay(5);

            window.Show();

            Apps.UpdateLayout();

            var container = Apps.ContainerFromItem(app);
            var targetElement = container.FindDescendant("Preview")!;
            var targetVisual = ElementCompositionPreview.GetElementVisual(targetElement);

            var targetPoint = targetElement.TransformToVisual(this).TransformPoint(new Point(0, 0));

            float scaleX = (float)(targetElement.ActualWidth / scaledSource.Width);
            float scaleY = (float)(targetElement.ActualHeight / scaledSource.Height);

            var easing = window.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.00f, 0.4f), new Vector2(0.2f, 1.0f));
            var duration = TimeSpan.FromMilliseconds(400);

            var scaleAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(1.0f, new(scaleX, scaleY, 1.0f), easing);
            scaleAnim.Duration = duration;

            var offsetAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(1.0f, new((float)targetPoint.X, (float)targetPoint.Y, 0.0f), easing);
            offsetAnim.Duration = duration;

            var snapEase = window.Compositor.CreateStepEasingFunction();
            snapEase.StepCount = 1;
            snapEase.IsFinalStepSingleFrame = true;

            var opacityAnim = window.Compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, snapEase);
            opacityAnim.Duration = duration;

            var animationGroup = window.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            thumbnail.Visual.StartAnimation("Scale", scaleAnim);
            thumbnail.Visual.StartAnimation("Offset", offsetAnim);
            targetVisual.StartAnimation("Opacity", opacityAnim);

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
            var factory = new UndockedDevKit.Wrapper.WindowThumbnailFactory(app.WindowId);

            var scaledSource = new Size(factory.SourceSize.Width / XamlRoot.RasterizationScale, factory.SourceSize.Height / XamlRoot.RasterizationScale);

            using var thumbnail = factory.CreateVisual(window.Compositor, scaledSource);

            var relativeX = factory.SourceBounds.X / XamlRoot.RasterizationScale - window.Bounds.X;
            var relativeY = factory.SourceBounds.Y / XamlRoot.RasterizationScale - window.Bounds.Y;

            var container = Apps.ContainerFromItem(app);
            var sourceElement = container.FindDescendant("Preview")!;
            var sourceVisual = ElementCompositionPreview.GetElementVisual(sourceElement);

            var targetPoint = sourceElement.TransformToVisual(this).TransformPoint(new Point(0, 0));

            float scaleX = (float)(sourceElement.ActualWidth / scaledSource.Width);
            float scaleY = (float)(sourceElement.ActualHeight / scaledSource.Height);

            thumbnail.Visual.Scale = new(scaleX, scaleY, 1.0f);
            thumbnail.Visual.Offset = new((float)targetPoint.X, (float)targetPoint.Y, 0.0f);

            ElementCompositionPreview.SetElementChildVisual(Foreground, thumbnail.Visual);

            await Task.Delay(5);

            var easing = window.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.00f, 0.4f), new Vector2(0.2f, 1.0f));
            var duration = TimeSpan.FromMilliseconds(400);

            var scaleAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(1.0f, new(1.0f, 1.0f, 1.0f), easing);
            scaleAnim.Duration = duration;

            var offsetAnim = window.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(1.0f, new((float)relativeX, (float)relativeY, 0.0f), easing);
            offsetAnim.Duration = duration;

            var opacityAnim = window.Compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 0.0f);
            opacityAnim.Duration = duration;

            var animationGroup = window.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            thumbnail.Visual.StartAnimation("Scale", scaleAnim);
            thumbnail.Visual.StartAnimation("Offset", offsetAnim);
            sourceVisual.StartAnimation("Opacity", opacityAnim);

            var tcs = new TaskCompletionSource();

            animationGroup.Completed += (s, e) => tcs.SetResult();
            animationGroup.End();

            await tcs.Task;

            sourceVisual.Opacity = 1.0f;
        }

        window.Hide();
        ElementCompositionPreview.SetElementChildVisual(Foreground, null);
        ElementCompositionPreview.SetElementChildVisual(Desktop, null);
        desktop?.Dispose();
        desktop = null;
    }

}
