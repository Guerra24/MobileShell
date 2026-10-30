#include "pch.h"
#include <winrt/WindowsUdk.UI.Shell.h>
#include "DuplicateVisual.h"
#include "DuplicateVisual.g.cpp"

namespace winrt::UndockedDevKit::Wrapper::implementation
{
	DuplicateVisual::DuplicateVisual()
	{
	}

	DuplicateVisual::DuplicateVisual(winrt::WindowsUdk::UI::Shell::DuplicateVisual const& visual) : m_udkVisual(visual)
	{
	}

	winrt::Windows::UI::Composition::Visual DuplicateVisual::Visual()
	{
		if (m_udkVisual) [[likely]]
		{
			return m_udkVisual.Visual();
		}
		else
		{
			return m_visual;
		}
	}

	void DuplicateVisual::Visual(winrt::Windows::UI::Composition::Visual const& visual, winrt::com_ptr<IDCompositionVisual2> const& dwmVisual)
	{
		m_visual = visual;
		m_dwmVisual = dwmVisual;
	}

	void implementation::DuplicateVisual::Close()
	{
		if (m_udkVisual) [[likely]]
		{
			m_udkVisual.Close();
			m_udkVisual = nullptr;
		}
		else
		{
			m_dwmVisual = nullptr;
			m_visual = nullptr;
		}
	}
}