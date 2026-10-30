#pragma once
#include <dcomp.h>
#include "DuplicateVisual.g.h"

namespace winrt::UndockedDevKit::Wrapper::implementation
{
    struct DuplicateVisual : DuplicateVisualT< DuplicateVisual>
    {
    private:
        winrt::Windows::UI::Composition::Visual m_visual{ nullptr };
        winrt::com_ptr<IDCompositionVisual2> m_dwmVisual{ nullptr };
        winrt::WindowsUdk::UI::Shell::DuplicateVisual m_udkVisual{ nullptr };

    public:
        DuplicateVisual();
        DuplicateVisual(winrt::WindowsUdk::UI::Shell::DuplicateVisual const& visual);

        winrt::Windows::UI::Composition::Visual Visual();
        void Visual(winrt::Windows::UI::Composition::Visual const& visual, winrt::com_ptr<IDCompositionVisual2> const& dwmVisual);
        void Close();
    };
}
