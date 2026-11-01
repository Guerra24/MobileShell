#include "pch.h"
#include <winrt/Windows.UI.Core.h>
#include "NetworkIcon.h"
#include "NetworkIcon.g.cpp"

namespace winrt::UndockedDevKit::Wrapper::implementation
{
	winrt::event<winrt::Windows::Foundation::EventHandler<winrt::WindowsUdk::UI::Shell::PrimaryIconInfoChangedEventArgs>> NetworkIcon::m_primaryIconInfoChanged;
	winrt::event_token NetworkIcon::m_targetToken{};

	void NetworkIcon::HookEvent()
	{
		if (m_targetToken) return;

		m_targetToken = winrt::WindowsUdk::UI::Shell::NetworkIcon::PrimaryIconInfoChanged([=](winrt::Windows::Foundation::IInspectable const& /*sender*/, winrt::WindowsUdk::UI::Shell::PrimaryIconInfoChangedEventArgs const& args)
			{
				if (m_primaryIconInfoChanged)
				{
					m_primaryIconInfoChanged(nullptr, args);
				}
			});
	}

	winrt::event_token NetworkIcon::PrimaryIconInfoChanged(winrt::Windows::Foundation::EventHandler<winrt::WindowsUdk::UI::Shell::PrimaryIconInfoChangedEventArgs> const& handler)
	{
		return m_primaryIconInfoChanged.add(handler);
	}

	void NetworkIcon::PrimaryIconInfoChanged(winrt::event_token const& token) noexcept
	{
		m_primaryIconInfoChanged.remove(token);
	}

	void NetworkIcon::UnhookEvent()
	{
		if (m_targetToken)
		{
			winrt::WindowsUdk::UI::Shell::NetworkIcon::PrimaryIconInfoChanged(m_targetToken);
			m_targetToken = {};
		}
	}
}
