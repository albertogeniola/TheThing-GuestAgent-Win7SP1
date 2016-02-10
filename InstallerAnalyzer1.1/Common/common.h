#ifdef COMMON_EXPORTS
#    define EXPORT __declspec(dllexport)
#else
#    define EXPORT __declspec(dllimport)
#endif

#pragma once

// Constants used for IPC among our processes
#define COPYDATA_LOG 0
#define COPYDATA_PROC_SPAWNED 1
#define COPYDATA_PROC_DIED 2

// Guest Controller window's name
#ifdef UNICODE
#define GUESTCONTROLLER_WINDOW_NAME L"WKWatcher"
#else
#define GUESTCONTROLLER_WINDOW_NAME "WKWatcher"
#endif


