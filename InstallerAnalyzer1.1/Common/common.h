#ifdef COMMON_EXPORTS
#    define EXPORT __declspec(dllexport)
#else
#    define EXPORT __declspec(dllimport)
#endif

#pragma once

// According to the MSDN documentation, this is the maximum path length
#define PATH_MAX_LEN 260

// Constants used for IPC among our processes
#define COPYDATA_LOG 0

#define COPYDATA_PROC_SPAWNED 1
#define COPYDATA_PROC_DIED 2

#define COPYDATA_FILE_CREATED 3
#define COPYDATA_FILE_DELETED 4
#define COPYDATA_FILE_OPENED 5
#define COPYDATA_FILE_RENAMED 6

#define COPYDATA_KEY_CREATED 10
#define COPYDATA_KEY_OPEN 11

// Guest Controller window's name
#ifdef UNICODE
#define GUESTCONTROLLER_WINDOW_NAME L"WKWatcher"

#else
#define GUESTCONTROLLER_WINDOW_NAME "WKWatcher"
#endif

#define DLLPATH "C:\\GuestController\\CHookingDll.dll"
#define DCOM_DLL_PATH "C:\\GuestController\\inject_dcom.dll"
#define DCOM_LAUNCH_SERVICE_NAME "DcomLaunch"

typedef struct srename_file_info {
	// Has to be null terminated!
	wchar_t oldPath[PATH_MAX_LEN];
	// Has to be null terminated!
	wchar_t newPath[PATH_MAX_LEN];
} rename_file_info;