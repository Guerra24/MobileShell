#include "pch.h"
#include <winrt/Windows.UI.Composition.h>
#include <winrt/Windows.ApplicationModel.Core.h>
#include <winrt/WindowsUdk.Foundation.Metadata.h>
#include <winrt/WindowsUdk.UI.Shell.h>
#include <dcomp.h>
#include "WindowThumbnailFactory.h"
#include "WindowThumbnailFactory.g.cpp"
#include "Helpers.h"
#include "DuplicateVisual.h"

namespace winrt::UndockedDevKit::Wrapper::implementation
{

	inline static const bool IsShellSwitchersContractAvailable = winrt::WindowsUdk::Foundation::Metadata::ApiInformation::IsApiContractPresent(L"WindowsUdk.UI.Shell.ShellSwitchersContract", 1);

	WindowThumbnailFactory::WindowThumbnailFactory(winrt::Windows::UI::WindowId const& windowId)
		: m_windowId(windowId)
	{
		if (IsShellSwitchersContractAvailable) [[likely]]
		{
			m_generator = winrt::WindowsUdk::UI::Shell::WindowThumbnailFactory(windowId);
		}
		else
		{
			m_hwnd = reinterpret_cast<HWND>(windowId.Value);

			winrt::check_hresult(DwmpQueryWindowThumbnailSourceSizeMethod(m_hwnd, FALSE, &m_size));

			m_sourceSize = { static_cast<float>(m_size.cx), static_cast<float>(m_size.cy) };

			if (IsIconic(m_hwnd))
			{
				WINDOWPLACEMENT wp =
				{
					sizeof(WINDOWPLACEMENT)
				};

				GetWindowPlacement(m_hwnd, &wp);
				m_bounds = wp.rcNormalPosition;
			}
			else
			{
				GetWindowRect(m_hwnd, &m_bounds);
			}

			m_sourceBounds = {
				static_cast<float>(m_bounds.left),
				static_cast<float>(m_bounds.top),
				static_cast<float>(m_bounds.right - m_bounds.left),
				static_cast<float>(m_bounds.bottom - m_bounds.top)
			};
		}
	}

	winrt::Windows::Foundation::Rect WindowThumbnailFactory::SourceBounds() const
	{
		if (IsShellSwitchersContractAvailable) [[likely]]
		{
			return m_generator.SourceBounds();
		}
		else
		{
			return m_sourceBounds;
		}
	}

	winrt::Windows::Foundation::Size WindowThumbnailFactory::SourceSize() const
	{
		if (IsShellSwitchersContractAvailable) [[likely]]
		{
			return m_generator.SourceSize();
		}
		else
		{
			return m_sourceSize;
		}
	}

	winrt::UndockedDevKit::Wrapper::DuplicateVisual WindowThumbnailFactory::CreateVisual(winrt::Windows::UI::Composition::Compositor const& compositor, winrt::Windows::Foundation::Size const& size) const
	{
		if (IsShellSwitchersContractAvailable) [[likely]]
		{
			return winrt::make_self<DuplicateVisual>(m_generator.CreateVisual(compositor, size)).as<winrt::UndockedDevKit::Wrapper::DuplicateVisual>();
		}
		else
		{
			auto dcompDevice = compositor.as<IDCompositionDesktopDevice>();

			HWND m_coreWindowHwnd{ nullptr };
			winrt::com_ptr<ICoreWindowInterop> interop = winrt::Windows::ApplicationModel::Core::CoreApplication::GetCurrentView().CoreWindow().as<ICoreWindowInterop>();
			winrt::check_hresult(interop->get_WindowHandle(&m_coreWindowHwnd));

			RECT dwmBounds;
			winrt::check_hresult(DwmGetWindowAttribute(m_hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, &dwmBounds, sizeof(RECT)));

			RECT source;
			if (IsIconic(m_hwnd))
			{
				source = {
					0,
					0,
					m_size.cx,
					m_size.cy
				};
			}
			else
			{
				source = {
					dwmBounds.left - m_bounds.left,
					dwmBounds.top - m_bounds.top,
					dwmBounds.right - m_bounds.left,
					dwmBounds.bottom - m_bounds.top
				};
			}

			DWM_THUMBNAIL_PROPERTIES thumb{};
			thumb.dwFlags = DWM_TNP_SOURCECLIENTAREAONLY | DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_RECTSOURCE | DWM_TNP_OPACITY | DWM_TNP_ENABLE3D;
			thumb.opacity = 255;
			thumb.fVisible = TRUE;
			thumb.fSourceClientAreaOnly = FALSE;
			thumb.rcDestination = { 0, 0, static_cast<LONG>(size.Width), static_cast<LONG>(size.Height) };
			thumb.rcSource = source;

			HTHUMBNAIL hThumbWindow;
			winrt::com_ptr<IDCompositionVisual2> windowVisual;
			winrt::check_hresult(DwmpCreateSharedThumbnailVisualMethod(GetParent(m_coreWindowHwnd), m_hwnd, 2, &thumb, dcompDevice.get(), windowVisual.put_void(), &hThumbWindow));

			winrt::com_ptr<IDCompositionVisual2> dcompVisualContainer;
			dcompDevice->CreateVisual(dcompVisualContainer.put());

			dcompVisualContainer->AddVisual(windowVisual.get(), TRUE, nullptr);
			auto visualContainer = dcompVisualContainer.as<winrt::Windows::UI::Composition::Visual>();

			visualContainer.Size(size);

			auto dupe = winrt::make_self<DuplicateVisual>();

			dupe->Visual(visualContainer, windowVisual);

			return dupe.as<winrt::UndockedDevKit::Wrapper::DuplicateVisual>();
		}
	}
}
