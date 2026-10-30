using TerraFX.Interop.Windows;
using Windows.Foundation;
using XamlHostingKit;
using static TerraFX.Interop.Windows.Windows;

namespace MobileShell.Extensions;

public static class XamlWindowExtensions
{
    public unsafe static uint AddAppBar(this XamlWindow window)
    {
        fixed (char* message = "AppBarMessage")
        {
            APPBARDATA abd = new()
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = (HWND)window.WindowHandle.Value,
                uCallbackMessage = RegisterWindowMessageW(message)
            };

            SHAppBarMessage(ABM.ABM_NEW, &abd);

            return abd.uCallbackMessage;
        }
    }

    public unsafe static void RemoveAppBar(this XamlWindow window)
    {
        APPBARDATA abd = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = (HWND)window.WindowHandle.Value
        };

        SHAppBarMessage(ABM.ABM_REMOVE, &abd);
    }

    public unsafe static void PositionAppBar(this XamlWindow window, HMONITOR monitor, float height, uint edge)
    {
        MONITORINFO mi = new()
        {
            cbSize = (uint)sizeof(MONITORINFO)
        };

        GetMonitorInfoW(monitor, &mi);

        var left = mi.rcMonitor.left;
        var top = mi.rcMonitor.top;
        var right = mi.rcMonitor.right;
        var bottom = mi.rcMonitor.bottom;

        APPBARDATA abd = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = (HWND)window.WindowHandle.Value,
            uEdge = edge
        };

        uint dpi;
        GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, &dpi, &dpi);
        float scale = dpi / 96.0f;

        abd.rc.left = left;
        abd.rc.right = right;

        switch (edge)
        {
            case ABE_TOP:
                abd.rc.top = top;
                abd.rc.bottom = (int)(top + height * scale);
                break;
            case ABE_BOTTOM:
                abd.rc.top = (int)(bottom - height * scale);
                abd.rc.bottom = bottom;
                break;
        }

        SHAppBarMessage(ABM.ABM_QUERYPOS, &abd);

        SHAppBarMessage(ABM.ABM_SETPOS, &abd);

        window.Resize((right - left) / scale, height);
        window.Move(abd.rc.left / scale, abd.rc.top / scale);
    }

    public unsafe static Rect MonitorRect(this HMONITOR monitor, float? scale = null)
    {
        MONITORINFO mi = new()
        {
            cbSize = (uint)sizeof(MONITORINFO)
        };

        GetMonitorInfoW(monitor, &mi);

        if (scale == null)
        {
            uint dpi;
            GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, &dpi, &dpi);
            scale = dpi / 96.0f;
        }

        var rc = mi.rcMonitor;

        var left = rc.left / scale.Value;
        var top = rc.top / scale.Value;
        var width = (rc.right - rc.left) / scale.Value;
        var height = (rc.bottom - rc.top) / scale.Value;

        return new Rect(left, top, width, height);
    }

    public unsafe static Rect MonitorWorkRect(this HMONITOR monitor)
    {
        MONITORINFO mi = new()
        {
            cbSize = (uint)sizeof(MONITORINFO)
        };

        GetMonitorInfoW(monitor, &mi);

        uint dpi;
        GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, &dpi, &dpi);
        var scale = dpi / 96.0f;

        var rc = mi.rcWork;

        var left = rc.left / scale;
        var top = rc.top / scale;
        var width = (rc.right - rc.left) / scale;
        var height = (rc.bottom - rc.top) / scale;

        return new Rect(left, top, width, height);
    }
}
