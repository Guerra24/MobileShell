#pragma once
#include <Windows.h>
#include <dwmapi.h>

namespace winrt::UndockedDevKit::Wrapper
{

	enum THUMBNAIL_TYPE
	{
		TT_DEFAULT = 0x0,
		TT_SNAPSHOT = 0x1,
		TT_ICONIC = 0x2,
		TT_BITMAPPENDING = 0x3,
		TT_BITMAP = 0x4
	};
	
	typedef HRESULT(WINAPI* DwmpCreateSharedThumbnailVisual)(
		IN HWND hwndDestination,
		IN HWND hwndSource,
		IN DWORD dwThumbnailFlags,
		IN DWM_THUMBNAIL_PROPERTIES* pThumbnailProperties,
		IN VOID* pDCompDevice,
		OUT VOID** ppVisual,
		OUT PHTHUMBNAIL phThumbnailId);

	typedef HRESULT(WINAPI* DwmpQueryWindowThumbnailSourceSize)(
		IN HWND hwndSource,
		IN BOOL fSourceClientAreaOnly,
		OUT SIZE* pSize);

	typedef HRESULT(WINAPI* DwmpQueryThumbnailType)(
		IN HTHUMBNAIL hThumbnailId,
		OUT THUMBNAIL_TYPE* thumbType);

#define DWM_TNP_ENABLE3D 0x4000000

	static const auto DwmapiModule = GetModuleHandleW(L"dwmapi.dll");

	static const auto DwmpQueryThumbnailTypeMethod = reinterpret_cast<DwmpQueryThumbnailType>(GetProcAddress(DwmapiModule, MAKEINTRESOURCEA(114)));

	static const auto DwmpCreateSharedThumbnailVisualMethod = reinterpret_cast<DwmpCreateSharedThumbnailVisual>(GetProcAddress(DwmapiModule, MAKEINTRESOURCEA(147)));

	static const auto DwmpQueryWindowThumbnailSourceSizeMethod = reinterpret_cast<DwmpQueryWindowThumbnailSourceSize>(GetProcAddress(DwmapiModule, MAKEINTRESOURCEA(162)));

}

MIDL_INTERFACE("45D64A29-A63E-4CB6-B498-5781D298CB4F")
ICoreWindowInterop : public IUnknown
{
public:
	virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WindowHandle(
		/* [retval][out] */ __RPC__deref_out_opt HWND * hwnd) = 0;

	virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MessageHandled(
		/* [in] */ boolean value) = 0;

};
