using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace MobileShell;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate LRESULT WNDPROC([In] HWND hWnd, [In] uint uMsg, [In] WPARAM wParam, [In] LPARAM lParam);

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate LRESULT SUBCLASSPROC([In] HWND hWnd, [In] uint uMsg, [In] WPARAM wParam, [In] LPARAM lParam, [In] nuint uIdSubclass, [In] nuint dwRefData);
