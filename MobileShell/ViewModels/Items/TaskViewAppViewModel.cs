using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MobileShell.Models;
using MobileShell.Services;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using TerraFX.Interop.Windows;
using UndockedDevKit.Wrapper;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell.ViewModels.Items;

public partial class TaskViewAppViewModel : ObservableObject
{

    private readonly TaskViewViewModel TaskViewViewModel;

    [ObservableProperty]
    public partial TaskViewAppEntry? TaskViewAppEntry { get; private set; }

    [ObservableProperty]
    public partial SoftwareBitmapSource? Icon { get; private set; }

    [ObservableProperty]
    public partial double Width { get; private set; }

    private DuplicateVisual? duplicateVisual;

    public TaskViewAppViewModel()
    {
        TaskViewViewModel = Service.TaskView[Service.Monitor.Value];
    }

    public async void Load(TaskViewAppEntry? app, UIElement preview)
    {
        ElementCompositionPreview.SetElementChildVisual(preview, null);

        duplicateVisual?.Dispose();
        duplicateVisual = null;
        Icon?.Dispose();
        Icon = null;
        TaskViewAppEntry = null;

        if (app == null || !TaskViewViewModel.AppList.Contains(app))
            return;

        TaskViewAppEntry = app;

        var factory = new WindowThumbnailFactory(app.WindowId);

        var ratio = factory.SourceSize.Width / factory.SourceSize.Height;

        var width = 500 * ratio;

        Width = width;

        var visual = ElementCompositionPreview.GetElementVisual(preview);

        duplicateVisual = factory.CreateVisual(visual.Compositor, new Size(width, 500));

        ElementCompositionPreview.SetElementChildVisual(preview, duplicateVisual.Visual);

        using var icon = LoadAppIcon(app.Handle);

        if (icon != null)
        {
            Icon = new();
            await Icon.SetBitmapAsync(icon);
        }
    }

    [RelayCommand]
    private void CloseWindow()
    {
        if (TaskViewAppEntry == null)
            return;

        TaskViewViewModel.AppList.Remove(TaskViewAppEntry);

        PostMessageW(TaskViewAppEntry.Handle, WM.WM_CLOSE, 0, 0);

        TaskViewAppEntry = null;
        duplicateVisual?.Dispose();
        duplicateVisual = null;
        Icon?.Dispose();
        Icon = null;
    }

    private static unsafe SoftwareBitmap? LoadAppIcon(HWND hwnd)
    {
        HICON hIcon;

        hIcon = (HICON)SendMessageW(hwnd, WM.WM_GETICON, ICON_BIG, 0);
        if (hIcon == HICON.NULL)
            hIcon = (HICON)SendMessageW(hwnd, WM.WM_GETICON, ICON_SMALL, 0);
        if (hIcon == HICON.NULL)
            hIcon = (HICON)GetClassLongPtrW(hwnd, GCLP_HICON);
        if (hIcon == HICON.NULL)
            hIcon = (HICON)GetClassLongPtrW(hwnd, GCLP_HICONSM);
        if (hIcon == HICON.NULL)
            return null;

        ICONINFOEXW iconInfo = new()
        {
            cbSize = (uint)sizeof(ICONINFOEXW)
        };

        if (!GetIconInfoExW(hIcon, &iconInfo))
            return null;

        try
        {
            var hBitmap = iconInfo.hbmColor;
            if (hBitmap == HBITMAP.NULL)
                return null;

            BITMAP bmpInfo;
            GetObjectW(hBitmap, sizeof(BITMAP), &bmpInfo);

            var width = bmpInfo.bmWidth;
            var height = bmpInfo.bmHeight;
            var byteCount = bmpInfo.bmWidthBytes * height;

            var rawBytes = NativeMemory.Alloc((nuint)byteCount);

            try
            {
                GetBitmapBits(hBitmap, byteCount, rawBytes);

                using var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Straight);
                using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
                using (var reference = buffer.CreateReference())
                {
                    WindowsRuntimeMarshal.TryGetDataUnsafe(reference, out var dataInBytes, out var capacityInBytes);
                    NativeMemory.Copy(rawBytes, (void*)dataInBytes, capacityInBytes);
                }

                var premultipliedBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                return premultipliedBitmap;
            }
            finally
            {
                NativeMemory.Free(rawBytes);
            }
        }
        finally
        {
            if (iconInfo.hbmColor != HBITMAP.NULL)
                DeleteObject(iconInfo.hbmColor);
            if (iconInfo.hbmMask != HBITMAP.NULL)
                DeleteObject(iconInfo.hbmMask);
        }
    }
}
