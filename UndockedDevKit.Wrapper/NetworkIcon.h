#pragma once
#include <winrt/WindowsUdk.UI.Shell.h>
#include "NetworkIcon.g.h"

namespace winrt::UndockedDevKit::Wrapper::implementation
{
    struct NetworkIcon
    {
    private:
        static winrt::event<winrt::Windows::Foundation::EventHandler<winrt::WindowsUdk::UI::Shell::PrimaryIconInfoChangedEventArgs>> m_primaryIconInfoChanged;
        static winrt::event_token m_targetToken;

    public:
        NetworkIcon() = delete;

        static void HookEvent(winrt::Windows::UI::Core::CoreDispatcher const& dispatcher);
        static winrt::event_token PrimaryIconInfoChanged(winrt::Windows::Foundation::EventHandler<winrt::WindowsUdk::UI::Shell::PrimaryIconInfoChangedEventArgs> const& handler);
        static void PrimaryIconInfoChanged(winrt::event_token const& token) noexcept;
        static void UnhookEvent();

    };
}

namespace winrt::UndockedDevKit::Wrapper::factory_implementation
{
    struct NetworkIcon : NetworkIconT<NetworkIcon, implementation::NetworkIcon>
    {
    };
}
