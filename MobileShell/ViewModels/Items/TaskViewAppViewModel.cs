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
using Windows.Management.Deployment;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using static TerraFX.Interop.Windows.ERROR;
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
    private PackageManager packageManager;

    public TaskViewAppViewModel()
    {
        TaskViewViewModel = Service.TaskView[Service.Monitor.Value];
        packageManager = new();
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

        using var icon = await LoadAppIcon(app.Handle);

        if (icon != null)
        {
            Icon = new();
            await Icon.SetBitmapAsync(icon);
        }
    }

    [RelayCommand]
    public void CloseWindow()
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

    private async Task<SoftwareBitmap?> LoadAppIcon(HWND hwnd)
    {
        if (IsWindowClass(hwnd, "ApplicationFrameWindow"))
        {
            var packageFullName = FindPackageFullName(hwnd);

            if (packageFullName == null)
                return null; // what

            var package = packageManager.FindPackageForUser(string.Empty, packageFullName);

            using var stream = await package.GetLogoAsRandomAccessStreamReference(new Size(32, 32)).OpenReadAsync();

            var decoder = await BitmapDecoder.CreateAsync(stream);

            return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        }
        else
        {
            return LoadWin32AppIcon(hwnd);
        }
    }

    private static unsafe string? FindPackageFullName(HWND hwnd)
    {
        uint framehostPid = 0;
        GetWindowThreadProcessId(hwnd, &framehostPid);

        var context = new TargetWindowContext
        {
            FramehostPid = framehostPid,
            CoreWindowPid = 0
        };

        EnumChildWindows(hwnd, &EnumChildWindowsCallback, (LPARAM)(&context));

        var process = OpenProcess(PROCESS.PROCESS_QUERY_LIMITED_INFORMATION, FALSE, context.CoreWindowPid);
        if (process == HANDLE.NULL)
            return null;
        try
        {
            uint length = 0;
            if (GetPackageFullName(process, &length, null) == ERROR_INSUFFICIENT_BUFFER)
            {
                var packageFullName = stackalloc char[(int)length];
                if (GetPackageFullName(process, &length, packageFullName) == 0)
                    return new(packageFullName);
                return null;
            }
            return null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    private static unsafe SoftwareBitmap? LoadWin32AppIcon(HWND hwnd)
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

    private struct TargetWindowContext
    {
        public uint FramehostPid;
        public uint CoreWindowPid;
    }

    [UnmanagedCallersOnly]
    private static unsafe BOOL EnumChildWindowsCallback(HWND hwnd, LPARAM lParam)
    {
        var context = (TargetWindowContext*)lParam.Value;

        uint pid = 0;
        GetWindowThreadProcessId(hwnd, &pid);

        if (pid != context->FramehostPid && IsWindowClass(hwnd, "Windows.UI.Core.CoreWindow"))
        {
            context->CoreWindowPid = pid;
            return FALSE;
        }

        return TRUE;
    }

    private static unsafe bool IsWindowClass(HWND hwnd, string className)
    {
        var windowClass = stackalloc char[256];
        var length = GetClassNameW(hwnd, windowClass, 256);
        return length > 0 && new string(windowClass, 0, length) == className;
    }
}
