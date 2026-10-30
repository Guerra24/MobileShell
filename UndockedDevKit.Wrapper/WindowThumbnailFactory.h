#pragma once
#include "WindowThumbnailFactory.g.h"

namespace winrt::UndockedDevKit::Wrapper::implementation
{
    struct WindowThumbnailFactory : WindowThumbnailFactoryT<WindowThumbnailFactory>
    {
    private:
        winrt::Windows::UI::WindowId m_windowId;
        winrt::Windows::Foundation::Rect m_sourceBounds;
        winrt::Windows::Foundation::Size m_sourceSize;
        SIZE m_size;
        RECT m_bounds;
        HWND m_hwnd;

        winrt::WindowsUdk::UI::Shell::WindowThumbnailFactory m_generator{ nullptr };

    public:
        WindowThumbnailFactory(winrt::Windows::UI::WindowId const& windowId);

        winrt::Windows::Foundation::Rect SourceBounds() const;
        winrt::Windows::Foundation::Size SourceSize() const;

        winrt::UndockedDevKit::Wrapper::DuplicateVisual CreateVisual(winrt::Windows::UI::Composition::Compositor const& compositor, winrt::Windows::Foundation::Size const& size) const;
    };
}

namespace winrt::UndockedDevKit::Wrapper::factory_implementation
{
    struct WindowThumbnailFactory : WindowThumbnailFactoryT<WindowThumbnailFactory, implementation::WindowThumbnailFactory>
    {
    };
}
